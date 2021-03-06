﻿using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Litigation_LitigationAnswered
    {
        public string TransactionHash { get; set; }
        public UInt64 BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public String OfferId { get; set; }
        public String HolderIdentity { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }
        public int BlockchainID { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Litigation_LitigationAnswered model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Litigation_LitigationAnswered WHERE TransactionHash = @hash AND BlockchainID = @blockchainID", new
            {
                hash = model.TransactionHash
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Litigation_LitigationAnswered
(TransactionHash, BlockNumber, Timestamp, OfferId, HolderIdentity, GasPrice, GasUsed, BlockchainID)
VALUES(@TransactionHash, @BlockNumber, @Timestamp, @OfferId, @HolderIdentity, @GasPrice, @GasUsed, @BlockchainID)",
                    new
                    {
                        model.TransactionHash,
                        model.BlockNumber,
                        model.Timestamp,
                        model.OfferId,
                        model.HolderIdentity,
                        model.GasPrice,
                        model.GasUsed,
                        model.BlockchainID
                    });

                OTOfferHolder.UpdateLitigationStatusesForOffer(connection, model.OfferId, model.BlockchainID);
            }
        }
    }
}