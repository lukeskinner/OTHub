﻿using System;
using Dapper;
using MySql.Data.MySqlClient;

namespace OTHub.BackendSync.Models.Database
{
    public class OTContract_Profile_TokensReleased
    {
        public String TransactionHash { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public String Profile { get; set; }
        public decimal Amount { get; set; }
        public ulong GasUsed { get; set; }
        public ulong GasPrice { get; set; }

        public static bool TransactionExists(MySqlConnection connection, string transactionHash)
        {
            var count = connection.QueryFirstOrDefault<Int32>(
                "SELECT COUNT(*) FROM OTContract_Profile_TokensReleased WHERE TransactionHash = @transactionHash", new
                {
                    transactionHash
                });

            if (count == 0)
                return false;

            return true;
        }

        public static void Insert(MySqlConnection connection, OTContract_Profile_TokensReleased model)
        {
            connection.Execute(
                @"INSERT INTO OTContract_Profile_TokensReleased(TransactionHash, ContractAddress, Profile, Amount, BlockNumber, GasPrice, GasUsed)
VALUES(@TransactionHash, @ContractAddress, @Profile, @Amount, @BlockNumber, @GasPrice, @GasUsed)",
                new
                {
                    model.TransactionHash,
                    model.ContractAddress,
                    model.Profile,
                    model.Amount,
                    model.BlockNumber,
                    model.GasPrice,
                    model.GasUsed
                });
        }
    }
}