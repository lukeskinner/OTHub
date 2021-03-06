﻿using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Holding_Paidout
    {
        public UInt64 ID { get; set; }
        public String OfferID { get; set; }
        public String Holder { get; set; }
        public Decimal Amount { get; set; }
        public Decimal? AmountInUSD { get; set; }
        public DateTime Timestamp { get; set; }
        public String TransactionHash { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public UInt64 GasUsed { get; set; }
        public string Data { get; set; }
        public UInt64 GasPrice { get; set; }
        public int BlockchainID { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Holding_Paidout model)
        {
            var count = connection.QueryFirstOrDefault<Int32>(@"SELECT COUNT(*) FROM OTContract_Holding_Paidout
WHERE OfferID = @offerID AND Holder = @holder AND TransactionHash = @hash AND Amount = @amount AND BlockchainID = @blockchainID", new
            {
                offerID = model.OfferID,
                holder = model.Holder,
                hash = model.TransactionHash,
                amount = model.Amount,
                blockchainID = model.BlockchainID
            });

            if (count == 0)
            {
                var close = connection.ExecuteScalar<Decimal?>($"(select Close from marketvaluebyday WHERE Date = '{model.Timestamp.Year}-{model.Timestamp.Month:00}-{model.Timestamp.Day:00}')");

                var inserted = connection.Execute(
                    @"
INSERT INTO OTContract_Holding_Paidout(OfferID, Holder, Amount, Timestamp, TransactionHash, ContractAddress, BlockNumber, AmountInUSD, GasUsed, Data, GasPrice, BlockchainID) 
VALUES(@OfferID, @Holder, @Amount, @Timestamp, @TransactionHash, @ContractAddress, @BlockNumber, @AmountInUSD, @GasUsed, @Data, @GasPrice, @BlockchainID)",
                    new
                    {
                        model.OfferID,
                        model.Holder,
                        model.Amount,
                        model.Timestamp,
                        model.TransactionHash,
                        model.ContractAddress,
                        model.BlockNumber,
                        AmountInUSD = close.HasValue ? close.Value * model.Amount : (Decimal?)null,
                        model.GasUsed,
                        model.Data,
                        model.GasPrice,
                        model.BlockchainID
                    });
            }
        }

        public static bool Exists(MySqlConnection connection, string offerId, string holder, decimal amount, string hash, int blockchainID)
        {
            var count = connection.QueryFirstOrDefault<Int32>(@"SELECT COUNT(*) FROM OTContract_Holding_Paidout
WHERE OfferID = @offerID AND Holder = @holder AND TransactionHash = @hash AND Amount = @amount AND BlockchainID = @blockchainID", new
            {
                offerID = offerId,
                holder = holder,
                hash = hash,
                amount = amount,
                blockchainID = blockchainID
            });

            return count > 0;
        }
    }
}