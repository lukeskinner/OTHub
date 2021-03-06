﻿using System;
using Dapper;
using MySqlConnector;

namespace OTHub.BackendSync.Database.Models
{
    public class OTContract_Approval_NodeRemoved
    {
        public String TransactionHash { get; set; }
        public String NodeId { get; set; }
        public String ContractAddress { get; set; }
        public UInt64 BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }

        public static void InsertIfNotExist(MySqlConnection connection, OTContract_Approval_NodeRemoved model)
        {
            var count = connection.QueryFirstOrDefault<Int32>("SELECT COUNT(*) FROM OTContract_Approval_NodeRemoved WHERE TransactionHash = @hash AND NodeId = @nodeId", new
            {
                hash = model.TransactionHash,
                nodeId = model.NodeId
            });

            if (count == 0)
            {
                connection.Execute(
                    @"INSERT INTO OTContract_Approval_NodeRemoved(TransactionHash, ContractAddress, NodeId, Timestamp, BlockNumber)
VALUES(@TransactionHash, @ContractAddress, @NodeId, @Timestamp, @BlockNumber)",
                    new
                    {
                        model.TransactionHash,
                        model.ContractAddress,
                        model.NodeId,
                        model.Timestamp,
                        model.BlockNumber
                    });
            }
        }
    }
}