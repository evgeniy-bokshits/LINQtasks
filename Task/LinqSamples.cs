// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;

// Version Mad01

namespace SampleQueries
{
	[Title("LINQ Module")]
	[Prefix("Linq")]
	public class LinqSamples : SampleHarness
	{
		private DataSource dataSource = new DataSource();

        [Category("Restriction Operators")]
        [Title("Where - Task 1")]
        [Description("1.Выдайте список всех клиентов, чей суммарный оборот (сумма всех заказов) превосходит некоторую величину X. Продемонстрируйте выполнение запроса с различными X (подумайте, можно ли обойтись без копирования запроса несколько раз)")]
        public void Linq1()
        {
            int[] values = new[] {1000, 5000, 15000};

            foreach (var v in values)
            {
                var customers =
                from c in dataSource.Customers
                where c.Orders.Sum(cp => cp.Total) > v
                select c;

                Console.WriteLine("Value - {0}", v);

                ObjectDumper.Write(customers);
            }
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 2")]
        [Description("2.Для каждого клиента составьте список поставщиков, находящихся в той же стране и том же городе. Сделайте задания с использованием группировки и без.")]
        public void Linq2()
        {
            var orders =
                from cust in dataSource.Customers
                from order in dataSource.Suppliers
                where cust.Country == order.Country && cust.City == order.City
                select new { CustomerName = cust.CompanyName, CustomerCountry = cust.Country, SupplierName = order.SupplierName, SupplierCountry = order.Country, SupplierCity = order.City};
            Console.WriteLine("Without Group:");
            ObjectDumper.Write(orders);
        }
        
        [Category("Restriction Operators")]
        [Title("Where - Task 3")]
        [Description("3. Найдите всех клиентов, у которых были заказы, превосходящие по сумме величину X")]
        public void Linq3()
        {
           int value = 10000;
            var customers =
                from cust in dataSource.Customers
                from ord in cust.Orders
                where ord.Total > value
                select cust;
            ObjectDumper.Write(customers);
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 4")]
        [Description("4.Выдайте список клиентов с указанием, начиная с какого месяца какого года они стали клиентами (принять за таковые месяц и год самого первого заказа)")]
        public void Linq4()
        {
            var customers =
                from cust in dataSource.Customers
                from ord in cust.Orders
                let z = cust.Orders.OrderBy(o => o.OrderDate.Date).First().OrderDate.Date
                where ord.OrderDate.Date == z
                select new {cust.CompanyName, z.Month, z.Year};
            ObjectDumper.Write(customers);
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 5")]
        [Description("5.Сделайте предыдущее задание, но выдайте список отсортированным по году, месяцу, оборотам клиента (от максимального к минимальному) и имени клиента")]
        public void Linq5()
        {
            var customers =
                from cust in dataSource.Customers
                let ob = cust.Orders.Sum(cp => cp.Total)
                from ord in cust.Orders
                let z = cust.Orders.OrderBy(o => o.OrderDate.Date).First().OrderDate.Date
                where ord.OrderDate.Date == z
                orderby z.Year, z.Month, ob descending, cust.CompanyName //!!!!по году, месяцу, оборотам клиента (от максимального к минимальному) и имени клиента
                select new { cust.CompanyName, z.Year, z.Month, ob };
            ObjectDumper.Write(customers);
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 6")]
        [Description("6.Укажите всех клиентов, у которых указан нецифровой код или не заполнен регион или в телефоне не указан код оператора (для простоты считаем, что это равнозначно «нет круглых скобочек в начале»).")]
        public void Linq6()
        {
            int tryParseRes;
            var customers =
                from cust in dataSource.Customers
                where Int32.TryParse(cust.PostalCode, out tryParseRes) == false || cust.Region == null || cust.Phone[0] != '('
                select new { cust.CompanyName, cust.PostalCode, cust.Region, cust.Phone };
            ObjectDumper.Write(customers);
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 7")]
        [Description("7.Сгруппируйте все продукты по категориям, внутри – по наличию на складе, внутри последней группы отсортируйте по стоимости")]
        public void Linq7()
        {
            var categoryGroup =
                from prod in dataSource.Products
                group prod by prod.Category
                into category
                select new
                {
                    CatKey = category.Key,
                    Categ = 
                        from cg in category
                        group cg by new {IsExist = cg.UnitsInStock != 0} into exGroup
                        select new
                        {
                            exGroup.Key,
                            Cont = 
                                from eg in exGroup
                                orderby eg.UnitPrice
                                select new
                                {
                                    eg.ProductName,
                                    eg.UnitPrice,
                                    eg.UnitsInStock,
                                    eg.ProductID
                                }
                        }
                };

            foreach (var cat in categoryGroup)
            {
                Console.WriteLine(cat.CatKey);
                foreach (var c in cat.Categ)
                {
                    Console.WriteLine("\t{0}",c.Key);
                    foreach (var r in c.Cont)
                    {
                        Console.WriteLine("\t\t ProductName - {0}", r.ProductName);
                        Console.WriteLine("\t\t UnitPrice -{0}", r.UnitPrice);
                        Console.WriteLine("\t\t UnitInStock - {0}", r.UnitsInStock);
                        Console.WriteLine("\t\t ProductId - {0}", r.ProductID);
                    }
                }
            }
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 8")]
        [Description("8.Сгруппируйте товары по группам «дешевые», «средняя цена», «дорогие». Границы каждой группы задайте сами.")]
        public void Linq8()
        {
            int[] cost = new[] {10, 50};

            var cheapProd =
                 from prod in dataSource.Products
                 where prod.UnitPrice <= cost[0]
                 select prod;


            var midProd =
                from prodm in dataSource.Products
                where prodm.UnitPrice <= cost[1] && prodm.UnitPrice > cost[0]
                select prodm;

            var expensiveProd =
                from prode in dataSource.Products
                where prode.UnitPrice > cost[1]
                select prode;

            Console.WriteLine("CheapCategory");
            ObjectDumper.Write(cheapProd, 2);

            Console.WriteLine("MidCategory");
            ObjectDumper.Write(midProd, 2);

            Console.WriteLine("ExpensiveCategory");
            ObjectDumper.Write(expensiveProd, 2);
        }

        [Category("Restriction Operators")]
        [Title("Where - Task 9")]
        [Description("9.Рассчитайте среднюю прибыльность каждого города (среднюю сумму заказа по всем клиентам из данного города) и среднюю интенсивность (среднее количество заказов, приходящееся на клиента из каждого города)")]
        public void Linq9()
        {
            var customers =
                from cust in dataSource.Customers
                group cust by cust.City
                into cityGroup
                select new
                {
                    cityGroup.Key,
                    CitySum =
                        from city in cityGroup
                        select city.Orders.Sum(s => s.Total),
                    CityOrd =
                        from city in cityGroup
                        select city.Orders.Length
                };

            var custAvg =
                from cust in customers
                select new
                {
                    City = cust.Key,
                    CityAverageProfitability = String.Format("{0:0.00}", cust.CitySum.Average()),
                    CityAverageIntensity = String.Format("{0:0.00}", cust.CityOrd.Average())
                };

            Console.WriteLine("Sum for each order in city");
            ObjectDumper.Write(customers, 1);

            Console.WriteLine("Средняя прибыльность и средняя интенсивность для каждого города");
            ObjectDumper.Write(custAvg,1);

        }

    }
}
