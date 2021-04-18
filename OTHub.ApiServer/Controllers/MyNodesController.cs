﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OTHub.Settings;

namespace OTHub.APIServer.Controllers
{
    [Route("api/[controller]")]
    public class MyNodesController : Controller
    {
        [HttpGet]
        [Authorize]
        [Route("JobsPerMonth")]
        public async Task<NodesPerYearMonthResponse> GetJobsPerMonth()
        {
            NodesPerYearMonthResponse response = new NodesPerYearMonthResponse();

            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                JobsPerMonthModel[] data = (await connection.QueryAsync<JobsPerMonthModel>(@"WITH JobsCTE AS (
SELECT 
i.NodeId, 
YEAR(o.FinalizedTimestamp) AS 'Year', 
MONTH(o.FinalizedTimestamp) AS 'Month',
SUM(o.TokenAmountPerHolder) AS TokenAmount,
COUNT(o.OfferID) AS JobCount,
SUM(ticker.Price * o.TokenAmountPerHolder) AS USDAmount
FROM otoffer o
JOIN otoffer_holders h ON h.OfferID = o.OfferID AND h.BlockchainID = o.BlockchainID
JOIN otidentity i ON i.Identity = h.Holder AND i.BlockchainID = o.BlockchainID
JOIN ticker_trac ticker ON ticker.Timestamp = (
SELECT MAX(TIMESTAMP)
FROM ticker_trac
WHERE TIMESTAMP <= o.FinalizedTimestamp)
GROUP BY i.NodeId, YEAR(o.FinalizedTimestamp), MONTH(o.FinalizedTimestamp)
)

SELECT mn.DisplayName, JobsCTE.*
FROM mynodes mn
JOIN JobsCTE ON mn.NodeId = JobsCTE.NodeID
WHERE mn.UserID = @userID
ORDER BY JobsCTE.NodeID, JobsCTE.Year, JobsCTE.Month", new
                {
                    userID = User.Identity.Name,
                })).ToArray();

                IEnumerable<IGrouping<string, JobsPerMonthModel>> groupedByNodes = data.GroupBy(m => m.NodeId);

                foreach (IGrouping<string, JobsPerMonthModel> nodeGroup in groupedByNodes)
                {
                    List<JobsPerYear> years = new List<JobsPerYear>();

                    IEnumerable<IGrouping<int, JobsPerMonthModel>> groupedByYears =
                        nodeGroup.GroupBy(g => g.Year).OrderBy(g => g.Key);

                    JobsPerMonth lastMonth = null;

                    foreach (IGrouping<int, JobsPerMonthModel> yearGroup in groupedByYears)
                    {
                        Dictionary<int, JobsPerMonthModel> months = yearGroup
                            .ToDictionary(k => k.Month, v => v);

                        JobsPerYear year = new JobsPerYear()
                        {
                            Year = yearGroup.Key.ToString(),
                            Active = yearGroup.Key == DateTime.Now.Year
                        };

                        for (int i = 1; i <= 12; i++)
                        {
                            JobsPerMonth month;

                            if (months.TryGetValue(i, out JobsPerMonthModel monthData))
                            {
                                month = new JobsPerMonth(monthData);
                            }
                            else
                            {
                                month = new JobsPerMonth()
                                {
                                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(i)
                                };
                            }

                            year.Months.Add(month);

                            if (lastMonth != null)
                            {
                                if (lastMonth.JobCount > month.JobCount || (lastMonth.JobCount == 0 && month.JobCount == 0))
                                {
                                    month.Down = true;
                                }
                            }
                            else
                            {
                                month.Down = true;
                            }

                            lastMonth = month;
                        }

                        years.Add(year);
                    }

                    response.Nodes.Add(new NodeJobsPerYear
                    {
                        NodeId = nodeGroup.Key,
                        DisplayName = nodeGroup.First().DisplayName,
                        Years = years
                    });
                }
            }

            JobsPerYear[] allYears = response.Nodes.SelectMany(n => n.Years).ToArray();

            IEnumerable<IGrouping<string, JobsPerYear>> allGroupedByYear = allYears.GroupBy(a => a.Year);

            response.AllNodes = new NodeJobsPerYear()
            {
                DisplayName = "All Nodes",
                Years = allGroupedByYear.Select(y => new JobsPerYear()
                {
                    Year = y.Key,
                    Active = y.First().Active,
                    Months = y.SelectMany(d => d.Months)
                        .GroupBy(m => m.Month)
                        .Select(m => new JobsPerMonth
                        {
                            Month = m.Key,
                            JobCount = m.Sum(d => d.JobCount),
                            TokenAmount = m.Sum(d => d.TokenAmount),
                            USDAmount = m.Sum(d => d.USDAmount)
                        })
                        .ToList()
                }).OrderBy(y => y.Year).ToList()
            };

            JobsPerMonth previousMonth = null;

            foreach (JobsPerYear year in response.AllNodes.Years)
            {
                foreach (JobsPerMonth month in year.Months)
                {
                    if (previousMonth == null)
                    {
                        month.Down = true;
                    }
                    else
                    {
                        if (previousMonth.JobCount == 0 && month.JobCount == 0)
                        {
                            month.Down = true;
                        }
                        else if (previousMonth.JobCount > month.JobCount)
                        {
                            month.Down = true;
                        }
                    }

                    previousMonth = month;
                }
            }

            return response;
        }

        [HttpPost]
        [Authorize]
        [Route("AddEditNode")]
        public async Task AddEditNode([FromQuery] string nodeID, [FromQuery] string name)
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                bool exists = await connection.ExecuteScalarAsync<bool>(@"SELECT 1 FROM MyNodes WHERE UserID = @userID AND NodeID = @nodeID", new
                {
                    userID = User.Identity.Name,
                    nodeID = nodeID
                });

                if (!exists)
                {
                    await connection.ExecuteAsync("INSERT INTO MyNodes (UserID, NodeID, DisplayName) VALUES (@userID, @nodeID, @name)", new
                    {
                        userID = User.Identity.Name,
                        nodeID = nodeID,
                        name = name
                    });
                }
                else
                {
                    await connection.ExecuteAsync("UPDATE MyNodes SET DisplayName = @name WHERE UserID = @userID AND NodeID = @nodeID", new
                    {
                        userID = User.Identity.Name,
                        nodeID = nodeID,
                        name = name
                    });
                }
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("DeleteNode")]
        public async Task DeleteNode([FromQuery]string nodeID)
        {
            await using (MySqlConnection connection =
                new MySqlConnection(OTHubSettings.Instance.MariaDB.ConnectionString))
            {
                bool exists = await connection.ExecuteScalarAsync<bool>(
                    @"SELECT 1 FROM MyNodes WHERE UserID = @userID AND NodeID = @nodeID", new
                    {
                        userID = User.Identity.Name,
                        nodeID = nodeID
                    });

                if (exists)
                {
                    await connection.ExecuteAsync("DELETE FROM MyNodes WHERE UserID = @userID AND NodeID = @nodeID", new
                    {
                        userID = User.Identity.Name,
                        nodeID = nodeID
                    });
                }
            }
        }
    }

    public class JobsPerMonthModel
    { 
        public string DisplayName { get; set; }
        public string NodeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TokenAmount { get; set; }
        public int JobCount { get; set; }
        public decimal USDAmount { get; set; }
    }

    public class NodesPerYearMonthResponse
    {
        public NodeJobsPerYear AllNodes { get; set; }
        public List<NodeJobsPerYear> Nodes { get; } = new List<NodeJobsPerYear>();
    }


    public class NodeJobsPerYear
    {
        public string DisplayName { get; set; }
        public string NodeId { get; set; }
        public List<JobsPerYear> Years { get; set; }
    }

    public class JobsPerYear
    {
        public bool Active { get; set; }
        public string Year { get; set; }

        public List<JobsPerMonth> Months { get; set; } = new List<JobsPerMonth>();
    }


    public class JobsPerMonth
    {
        public JobsPerMonth()
        {
            
        }
        public JobsPerMonth(JobsPerMonthModel model)
        {
            Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(model.Month);
            TokenAmount = model.TokenAmount;
            JobCount = model.JobCount;
            USDAmount = model.USDAmount;
        }

        public string Month { get; set; }
        public decimal TokenAmount { get; set; }
        public int JobCount { get; set; }
        public decimal USDAmount { get; set; }
        public bool Down { get; set; }
    }
}