﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra
{
    public partial class RinhaBackendDbContextModel
    {
        partial void Initialize()
        {
            var transaction = TransactionEntityType.Create(this);

            TransactionEntityType.CreateAnnotations(transaction);

            AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            AddAnnotation("ProductVersion", "8.0.2");
            AddAnnotation("Relational:MaxIdentifierLength", 63);
            AddRuntimeAnnotation("Relational:RelationalModel", CreateRelationalModel());
        }

        private IRelationalModel CreateRelationalModel()
        {
            var relationalModel = new RelationalModel(this);

            var transaction = FindEntityType("Awarean.BrayaOrtega.RinhaBackend.Q124.Transaction")!;

            var defaultTableMappings = new List<TableMappingBase<ColumnMappingBase>>();
            transaction.SetRuntimeAnnotation("Relational:DefaultMappings", defaultTableMappings);
            var awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase = new TableBase("Awarean.BrayaOrtega.RinhaBackend.Q124.Transaction", null, relationalModel);
            var accountidColumnBase = new ColumnBase<ColumnMappingBase>("accountid", "integer", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase);
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.Columns.Add("accountid", accountidColumnBase);
            var descricaoColumnBase = new ColumnBase<ColumnMappingBase>("descricao", "varchar(10)", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase)
            {
                IsNullable = true
            };
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.Columns.Add("descricao", descricaoColumnBase);
            var limiteColumnBase = new ColumnBase<ColumnMappingBase>("limite", "integer", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase);
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.Columns.Add("limite", limiteColumnBase);
            var realizadaemColumnBase = new ColumnBase<ColumnMappingBase>("realizadaem", "timestamp with time zone", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase);
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.Columns.Add("realizadaem", realizadaemColumnBase);
            var saldoColumnBase = new ColumnBase<ColumnMappingBase>("saldo", "integer", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase);
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.Columns.Add("saldo", saldoColumnBase);
            var tipoColumnBase = new ColumnBase<ColumnMappingBase>("tipo", "varchar(1)", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase);
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.Columns.Add("tipo", tipoColumnBase);
            var valorColumnBase = new ColumnBase<ColumnMappingBase>("valor", "integer", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase);
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.Columns.Add("valor", valorColumnBase);
            relationalModel.DefaultTables.Add("Awarean.BrayaOrtega.RinhaBackend.Q124.Transaction", awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase);
            var awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase = new TableMappingBase<ColumnMappingBase>(transaction, awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase, true);
            awareanBrayaOrtegaRinhaBackendQ124TransactionTableBase.AddTypeMapping(awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase, false);
            defaultTableMappings.Add(awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)accountidColumnBase, transaction.FindProperty("AccountId")!, awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)descricaoColumnBase, transaction.FindProperty("Descricao")!, awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)limiteColumnBase, transaction.FindProperty("Limite")!, awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)realizadaemColumnBase, transaction.FindProperty("RealizadaEm")!, awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)saldoColumnBase, transaction.FindProperty("Saldo")!, awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)tipoColumnBase, transaction.FindProperty("Tipo")!, awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)valorColumnBase, transaction.FindProperty("Valor")!, awareanBrayaOrtegaRinhaBackendQ124TransactionMappingBase);

            var tableMappings = new List<TableMapping>();
            transaction.SetRuntimeAnnotation("Relational:TableMappings", tableMappings);
            var transactionsTable = new Table("transactions", null, relationalModel);
            var accountidColumn = new Column("accountid", "integer", transactionsTable);
            transactionsTable.Columns.Add("accountid", accountidColumn);
            var descricaoColumn = new Column("descricao", "varchar(10)", transactionsTable)
            {
                IsNullable = true
            };
            transactionsTable.Columns.Add("descricao", descricaoColumn);
            var limiteColumn = new Column("limite", "integer", transactionsTable);
            transactionsTable.Columns.Add("limite", limiteColumn);
            var realizadaemColumn = new Column("realizadaem", "timestamp with time zone", transactionsTable);
            transactionsTable.Columns.Add("realizadaem", realizadaemColumn);
            var saldoColumn = new Column("saldo", "integer", transactionsTable);
            transactionsTable.Columns.Add("saldo", saldoColumn);
            var tipoColumn = new Column("tipo", "varchar(1)", transactionsTable);
            transactionsTable.Columns.Add("tipo", tipoColumn);
            var valorColumn = new Column("valor", "integer", transactionsTable);
            transactionsTable.Columns.Add("valor", valorColumn);
            relationalModel.Tables.Add(("transactions", null), transactionsTable);
            var transactionsTableMapping = new TableMapping(transaction, transactionsTable, true);
            transactionsTable.AddTypeMapping(transactionsTableMapping, false);
            tableMappings.Add(transactionsTableMapping);
            RelationalModel.CreateColumnMapping(accountidColumn, transaction.FindProperty("AccountId")!, transactionsTableMapping);
            RelationalModel.CreateColumnMapping(descricaoColumn, transaction.FindProperty("Descricao")!, transactionsTableMapping);
            RelationalModel.CreateColumnMapping(limiteColumn, transaction.FindProperty("Limite")!, transactionsTableMapping);
            RelationalModel.CreateColumnMapping(realizadaemColumn, transaction.FindProperty("RealizadaEm")!, transactionsTableMapping);
            RelationalModel.CreateColumnMapping(saldoColumn, transaction.FindProperty("Saldo")!, transactionsTableMapping);
            RelationalModel.CreateColumnMapping(tipoColumn, transaction.FindProperty("Tipo")!, transactionsTableMapping);
            RelationalModel.CreateColumnMapping(valorColumn, transaction.FindProperty("Valor")!, transactionsTableMapping);
            return relationalModel.MakeReadOnly();
        }
    }
}
