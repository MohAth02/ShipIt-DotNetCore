﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Npgsql;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;

namespace ShipIt.Repositories
{
    public interface IStockRepository
    {
        int GetTrackedItemsCount();
        int GetStockHeldSum();
        IEnumerable<StockDataModel> GetStockByWarehouseId(int id);
        IEnumerable<InboundOrderDataModel> GetInboundOrderDataByWarehouseId(int warehouseId);
        Dictionary<int, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<int> productIds);
        void RemoveStock(int warehouseId, List<StockAlteration> lineItems);
        void AddStock(int warehouseId, List<StockAlteration> lineItems);
    }

    public class StockRepository : RepositoryBase, IStockRepository
    {

        public int GetTrackedItemsCount()
        {
            string sql = "SELECT COUNT(*) FROM stock";
            return (int)QueryForLong(sql);
        }

        public int GetStockHeldSum()
        {
            string sql = "SELECT SUM(hld) FROM stock";
            return (int)QueryForLong(sql);
        }

        public IEnumerable<StockDataModel> GetStockByWarehouseId(int id)
        {
            string sql = "SELECT p_id, hld, w_id FROM stock WHERE w_id = @w_id";
            var parameter = new NpgsqlParameter("@w_id", id);
            string noProductWithIdErrorMessage = string.Format("No stock found with w_id: {0}", id);
            try
            {
                return base.RunGetQuery(sql, reader => new StockDataModel(reader), noProductWithIdErrorMessage, parameter).ToList();
            }
            catch (NoSuchEntityException)
            {
                return new List<StockDataModel>();
            }
        }

        public IEnumerable<InboundOrderDataModel> GetInboundOrderDataByWarehouseId(int warehouseId)
        {
            string sql = @"
                SELECT
                    s.hld,
                    p.gtin_cd,
                    p.gtin_nm,
                    p.l_th,
                    p.min_qt,
                    c.gcp_cd,
                    c.gln_nm,
                    c.gln_addr_02,
                    c.gln_addr_03,
                    c.gln_addr_04,
                    c.gln_addr_postalcode,
                    c.gln_addr_city,
                    c.contact_tel,
                    c.contact_mail
                FROM stock s
                INNER JOIN gtin p ON p.p_id = s.p_id
                INNER JOIN gcp c ON c.gcp_cd = p.gcp_cd
                WHERE s.w_id = @w_id
                  AND s.hld < p.l_th
                  AND p.ds = 0";

            var parameter = new NpgsqlParameter("@w_id", warehouseId);

            try
            {
                return RunGetQuery(
                    sql,
                    reader => new InboundOrderDataModel(reader),
                    $"No inbound order data found for warehouse: {warehouseId}",
                    parameter)
                    .ToList();
            }
            catch (NoSuchEntityException)
            {
                return new List<InboundOrderDataModel>();
            }
        }

        public Dictionary<int, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<int> productIds)
        {
            string sql = string.Format("SELECT p_id, hld, w_id FROM stock WHERE w_id = @w_id AND p_id IN ({0})",
                String.Join(",", productIds));
            var parameter = new NpgsqlParameter("@w_id", warehouseId);
            string noProductWithIdErrorMessage = string.Format("No stock found with w_id: {0} and p_ids: {1}",
                warehouseId, String.Join(",", productIds));
            var stock = base.RunGetQuery(sql, reader => new StockDataModel(reader), noProductWithIdErrorMessage, parameter);
            return stock.ToDictionary(s => s.ProductId, s => s);
        }
            
        public void AddStock(int warehouseId, List<StockAlteration> lineItems)
        {
            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var orderLine in lineItems)
            {
                parametersList.Add(
                    new NpgsqlParameter[] {
                        new NpgsqlParameter("@p_id", orderLine.ProductId),
                        new NpgsqlParameter("@w_id", warehouseId),
                        new NpgsqlParameter("@hld", orderLine.Quantity)
                    });
            }

            string sql = "INSERT INTO stock (p_id, w_id, hld) VALUES (@p_id, @w_id, @hld) "
                         + "ON CONFLICT (p_id, w_id) DO UPDATE SET hld = stock.hld + EXCLUDED.hld";

            var recordsAffected = new List<int>();
            foreach (var parameters in parametersList)
            {
                 recordsAffected.Add(
                     RunSingleQueryAndReturnRecordsAffected(sql, parameters)
                 );
            }

            string errorMessage = null;

            for (int i = 0; i < recordsAffected.Count; i++)
            {
                if (recordsAffected[i] == 0)
                {
                    errorMessage = String.Format("Product {0} in warehouse {1} was unexpectedly not updated (rows updated returned {2})",
                        parametersList[i][0], warehouseId, recordsAffected[i]);
                }
            }

            if (errorMessage != null)
            {
                throw new InvalidStateException(errorMessage);
            }
        }

        public void RemoveStock(int warehouseId, List<StockAlteration> lineItems)
        {
            string sql = string.Format("UPDATE stock SET hld = hld - @hld WHERE w_id = {0} AND p_id = @p_id",
                warehouseId);

            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var lineItem in lineItems)
            {
                parametersList.Add(new NpgsqlParameter[]
                {
                    new NpgsqlParameter("@hld", lineItem.Quantity),
                    new NpgsqlParameter("@p_id", lineItem.ProductId)
                });
            }

            base.RunTransaction(sql, parametersList);
        }
    }
}