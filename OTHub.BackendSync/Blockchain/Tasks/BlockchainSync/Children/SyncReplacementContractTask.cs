﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.RPC.Eth.DTOs;
using OTHub.BackendSync.Database.Models;
using OTHub.BackendSync.Logging;
using OTHub.Settings;
using OTHub.Settings.Abis;

namespace OTHub.BackendSync.Blockchain.Tasks.BlockchainSync.Children
{
    public class SyncReplacementContractTask : TaskRunBlockchain
    {
        public SyncReplacementContractTask() : base("Sync Replacement Contract")
        {
        }

        public override async Task Execute(Source source, BlockchainType blockchain, BlockchainNetwork network)
        {
            ClientBase.ConnectionTimeout = new TimeSpan(0, 0, 5, 0);

            using (var connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                //OTContract litigationStorageContract = OTContract.GetByType(connection, (int) ContractType.LitigationStorage).FirstOrDefault(c => c.IsLatest);

                //Contract storageContract = new Contract((EthApiService)cl.Eth, Constants.GetContractAbi(ContractType.LitigationStorage), litigationStorageContract.Address);
                // Function getLitigationStatusFunction = storageContract.GetFunction("getLitigationStatus");

                int blockchainID = GetBlockchainID(connection, blockchain, network);

                var cl = GetWeb3(connection, blockchainID);
                var eth = new EthApiService(cl.Client);

                foreach (var contract in OTContract.GetByTypeAndBlockchain(connection, (int)ContractTypeEnum.Replacement, blockchainID))
                {
                    if (contract.IsArchived && contract.LastSyncedTimestamp.HasValue &&
                        (DateTime.Now - contract.LastSyncedTimestamp.Value).TotalDays <= 5)
                    {
#if DEBUG
                        Logger.WriteLine(source, "     Skipping contract: " + contract.Address);
#endif
                        continue;
                    }

                    Logger.WriteLine(source, "     Using contract: " + contract.Address);

                    var holdingContract = new Contract(eth, AbiHelper.GetContractAbi(ContractTypeEnum.Replacement, blockchain, network), contract.Address);

                    var replacementCompletedEvent = holdingContract.GetEvent("ReplacementCompleted");

                    var toBlock = new BlockParameter(LatestBlockNumber);

                    var replacementCompletedEvents = await replacementCompletedEvent.GetAllChangesDefault(
                        replacementCompletedEvent.CreateFilterInput(new BlockParameter(contract.SyncBlockNumber),
                            toBlock));


                    if (replacementCompletedEvents.Any())
                    {
                        Logger.WriteLine(source, "Found " + replacementCompletedEvents.Count + " replacement completed events");
                    }

                    foreach (EventLog<List<ParameterOutput>> eventLog in replacementCompletedEvents)
                    {

                        var block = await BlockHelper.GetBlock(connection, eventLog.Log.BlockHash, eventLog.Log.BlockNumber,
                            cl, blockchainID);
                        var offerId =
                            HexHelper.ByteArrayToString((byte[])eventLog.Event
                                .First(e => e.Parameter.Name == "offerId").Result);

                        var challengerIdentity = (string)eventLog.Event
                                .First(e => e.Parameter.Name == "challengerIdentity").Result;

                        var chosenHolder = (string)eventLog.Event
                                .First(e => e.Parameter.Name == "chosenHolder").Result;

                        var transaction = await eth.Transactions.GetTransactionByHash.SendRequestAsync(eventLog.Log.TransactionHash);
                        var receipt = await eth.Transactions.GetTransactionReceipt.SendRequestAsync(eventLog.Log.TransactionHash);

                        var row = new OTContract_Replacement_ReplacementCompleted
                        {
                            TransactionHash = eventLog.Log.TransactionHash,
                            BlockNumber = (UInt64)eventLog.Log.BlockNumber.Value,
                            Timestamp = block.Timestamp,
                            OfferId = offerId,
                            ChosenHolder = chosenHolder,
                            ChallengerIdentity = challengerIdentity,
                            GasPrice = (UInt64)transaction.GasPrice.Value,
                            GasUsed = (UInt64)receipt.GasUsed.Value,
                            BlockchainID = blockchainID
                        };

                        OTContract_Replacement_ReplacementCompleted.InsertIfNotExist(connection, row);

                        OTOfferHolder.Insert(connection, offerId, chosenHolder, false, blockchainID);

                        OTOfferHolder.UpdateLitigationStatusesForOffer(connection, offerId, blockchainID);
                    }

                    contract.LastSyncedTimestamp = DateTime.Now;
                    contract.SyncBlockNumber = (ulong)toBlock.BlockNumber.Value;

                    OTContract.Update(connection, contract, false, false);
                }
            }
        }
    }
}