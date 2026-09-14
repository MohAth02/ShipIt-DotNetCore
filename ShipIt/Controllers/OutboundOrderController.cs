﻿using System;
using System.Collections.Generic;
using System.Linq;
 using Microsoft.AspNetCore.Mvc;
 using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{
    [Route("orders/outbound")]
    public class OutboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        [HttpPost("")]
        public OutBoundResponse Post([FromBody] OutboundOrderRequestModel request)
        {
            Log.Info(String.Format("Processing outbound order: {0}", request));

            var gtins = new List<String>();
            foreach (var orderLine in request.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(String.Format("Outbound order request contains duplicate product gtin: {0}", orderLine.gtin));
                }
                gtins.Add(orderLine.gtin);
            }

            var productDataModels = _productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            var lineItems = new List<StockAlteration>();
            var productIds = new List<int>();
            var errors = new List<string>();
            var TotalWeightKg = 0.0;
            

            foreach (var orderLine in request.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add(string.Format("Unknown product gtin: {0}", orderLine.gtin));
                }
                else
                {
                    TotalWeightKg += (products[orderLine.gtin].Weight * orderLine.quantity)/1000;
                    var product = products[orderLine.gtin];
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                    productIds.Add(product.Id);
                }
            }
            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            var stock = _stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId, productIds);

            var orderLines = request.OrderLines.ToList();
            errors = new List<string>();

            for (int i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                var orderLine = orderLines[i];

                if (!stock.ContainsKey(lineItem.ProductId))
                {
                    errors.Add(string.Format("Product: {0}, no stock held", orderLine.gtin));
                    continue;
                }

                var item = stock[lineItem.ProductId];
                if (lineItem.Quantity > item.held)
                {
                    errors.Add(
                        string.Format("Product: {0}, stock held: {1}, stock to remove: {2}", orderLine.gtin, item.held,
                            lineItem.Quantity));
                }
            }

            if (errors.Count > 0)
            {
                throw new InsufficientStockException(string.Join("; ", errors));
            }

            var trucks = CreateTrucks(request.OrderLines, products);
            _stockRepository.RemoveStock(request.WarehouseId, lineItems);

            var response = new OutBoundResponse()
            {
                TotalWeightKg = TotalWeightKg,
                TrucksRequired = trucks.Count,
                Trucks = trucks
            };

            return response;
        }

        private List<OutboundTruck> CreateTrucks(
            IEnumerable<OrderLine> orderLines,
            Dictionary<string, Product> products)
        {
            var trucks = new List<OutboundTruck>();
            var sortedOrderLines = orderLines
                .OrderByDescending(orderLine =>
                    products[orderLine.gtin].Weight * orderLine.quantity)
                .ToList();

            foreach (var orderLine in sortedOrderLines)
            {
                var product = products[orderLine.gtin];
                var unitWeightKg = product.Weight / 1000;
                var remainingQuantity = orderLine.quantity;

                if (unitWeightKg > 2000)
                {
                    throw new ValidationException(
                        string.Format("Product {0} weighs more than the truck capacity", orderLine.gtin));
                }

                while (remainingQuantity > 0)
                {
                    var truck = trucks
                        .Where(t => 2000 - t.TotalWeightKg >= unitWeightKg)
                        .OrderByDescending(t => t.TotalWeightKg)
                        .FirstOrDefault();

                    if (truck == null)
                    {
                        truck = new OutboundTruck();
                        trucks.Add(truck);
                    }

                    var availableCapacity = 2000 - truck.TotalWeightKg;
                    var quantityThatFits = (int)Math.Floor(availableCapacity / unitWeightKg);
                    var quantityToAdd = Math.Min(remainingQuantity, quantityThatFits);

                    truck.OrderLines.Add(new OrderLine
                    {
                        gtin = orderLine.gtin,
                        quantity = quantityToAdd
                    });
                    truck.TotalWeightKg += quantityToAdd * unitWeightKg;
                    remainingQuantity -= quantityToAdd;
                }
            }

            return trucks;
        }
    }
}
