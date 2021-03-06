﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children
{
    public class ProcessJobsTask : TaskRunBlockchain
    {
        public ProcessJobsTask() : base("Process Jobs")
        {
        }

        public override async Task Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            await using (var connection = new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                int blockchainID = GetBlockchainID(connection, blockchain, network);
                await Execute(connection, blockchainID, blockchain, network);
            }
        }

        public static async Task Execute(MySqlConnection connection, int blockchainID, BlockchainType blockchain, BlockchainNetwork network)
        {
            using (await LockManager.GetLock(LockType.ProcessJobs).Lock())
            {
                OTContract_Holding_OfferCreated[] offersToAdd =
                    OTContract_Holding_OfferCreated.GetUnprocessed(connection, blockchainID);

                if (offersToAdd.Any())
                {
                    Console.WriteLine("Found " + offersToAdd.Length + " unprocessed offer created events.");
                }

                foreach (var offerToAdd in offersToAdd)
                {
                    OTOffer offer = new OTOffer
                    {
                        CreatedTimestamp = offerToAdd.Timestamp,
                        OfferID = offerToAdd.OfferID,
                        CreatedTransactionHash = offerToAdd.TransactionHash,
                        CreatedBlockNumber = offerToAdd.BlockNumber,
                        DCNodeId = offerToAdd.DCNodeId,
                        DataSetId = offerToAdd.DataSetId,
                        DataSetSizeInBytes = offerToAdd.DataSetSizeInBytes,
                        HoldingTimeInMinutes = offerToAdd.HoldingTimeInMinutes,
                        IsFinalized = false,
                        LitigationIntervalInMinutes = offerToAdd.LitigationIntervalInMinutes,
                        TransactionIndex = offerToAdd.TransactionIndex,
                        TokenAmountPerHolder = offerToAdd.TokenAmountPerHolder,
                        BlockchainID = blockchainID
                    };

                    OTOffer.InsertIfNotExist(connection, offer);

                    OTContract_Holding_OfferCreated.SetProcessed(connection, offerToAdd);
                }

                OTContract_Holding_OfferFinalized[] offersToFinalize =
                    OTContract_Holding_OfferFinalized.GetUnprocessed(connection, blockchainID);

                if (offersToFinalize.Any())
                {
                    Console.WriteLine("Found " + offersToFinalize.Length + " unprocessed offer finalized events.");
                }

                foreach (var offerToFinalize in offersToFinalize)
                {
                    OTOffer.FinalizeOffer(connection, offerToFinalize.OfferID, offerToFinalize.BlockNumber,
                        offerToFinalize.TransactionHash, offerToFinalize.Holder1, offerToFinalize.Holder2,
                        offerToFinalize.Holder3, offerToFinalize.Timestamp, blockchainID);

                    OTContract_Holding_OfferFinalized.SetProcessed(connection, offerToFinalize);
                }
            }
        }
    }
}