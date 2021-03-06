﻿using Dapper;
using LiteGuard;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ZendeskApi_v2.Models.Tickets;

namespace ZendeskTicketExporter.Core
{
    public class SQLiteMergedTicketExporter : IMergedTicketExporter
    {
        private static readonly PropertyInfo[] TicketProperties = typeof(TicketExportResult).GetProperties();

        private readonly IDatabase _database;

        public SQLiteMergedTicketExporter(IDatabase database)
        {
            _database = database;
        }

        public async Task WriteAsync(IEnumerable<TicketExportResult> tickets)
        {
            Guard.AgainstNullArgument("tickets", tickets);

            await _database.ExecuteAsync(string.Format(
                "create table if not exists {0} ({1}, primary key (Id));",
                Configuration.TicketsTableName,
                string.Join(", ", TicketProperties.Select(x => x.Name))));

            foreach (var ticket in tickets)
            {
                var insertParams = new DynamicParameters();
                foreach (var property in TicketProperties)
                    insertParams.Add(property.Name, property.GetValue(ticket));

                await _database.ExecuteAsync(
                    string.Format("insert or replace into {0} ({1}) values ({2})",
                        Configuration.TicketsTableName,
                        string.Join(", ", TicketProperties.Select(x => x.Name)),
                        string.Join(", ", TicketProperties.Select(x => "@" + x.Name))),
                    insertParams);
            }
        }
    }
}