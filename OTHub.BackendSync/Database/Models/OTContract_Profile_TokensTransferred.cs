﻿using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Profile_TokensTransferred
    {
        public String TransactionHash { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public String Sender { get; set; }
        public String Receiver { get; set; }
        public decimal Amount { get; set; }
        public ulong GasPrice { get; set; }
        public ulong GasUsed { get; set; }
        public int BlockchainID { get; set; }

        public static bool TransactionExists(MySqlConnection connection, string transactionHash, int blockchainID)
        {
            var count = connection.QueryFirstOrDefault<Int32>(
                "SELECT COUNT(*) FROM OTContract_Profile_TokensTransferred WHERE TransactionHash = @transactionHash AND BlockchainID = @blockchainID", new
                {
                    transactionHash,
                    blockchainID = blockchainID
                });

            if (count == 0)
                return false;

            return true;
        }

        public static void Insert(MySqlConnection connection, OTContract_Profile_TokensTransferred model)
        {
            connection.Execute(
                @"INSERT INTO OTContract_Profile_TokensTransferred(TransactionHash, ContractAddress, Sender, Receiver, Amount, BlockNumber, GasUsed, GasPrice, BlockchainID)
VALUES(@TransactionHash, @ContractAddress, @Sender, @Receiver, @Amount, @BlockNumber, @GasUsed, @GasPrice, @BlockchainID)",
                new
                {
                    model.TransactionHash,
                    model.ContractAddress,
                    model.Sender,
                    model.Receiver,
                    model.Amount,
                    model.BlockNumber,
                    model.GasPrice,
                    model.GasUsed,
                    model.BlockchainID
                });
        }
    }
}