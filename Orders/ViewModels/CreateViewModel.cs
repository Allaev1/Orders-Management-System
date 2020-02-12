﻿using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Linq;
using System.Reactive;
using DataAccessLocal;
using ReactiveUI.Fody.Helpers;

namespace Orders.ViewModels
{
    public class CreateViewModel : ReactiveObject, INavigationAware, IRegionMemberLifetime
    {
        #region Declarations
        NorthwindContext northwindContext;

        ReadOnlyObservableCollection<ProductInOrder> _productsInOrder;
        ReadOnlyObservableCollection<ProductOnStore> _productsInStore;
        ReadOnlyObservableCollection<Employee> _employees;
        ReadOnlyObservableCollection<Customer> _customers;

        SourceCache<Product, int> products;
        SourceList<ProductInOrder> productsInOrder;
        SourceList<Employee> employees;
        SourceList<Customer> customers;
        #endregion

        public CreateViewModel(NorthwindContext northwindContext)
        {
            this.northwindContext = northwindContext;

            products = new SourceCache<Product, int>(p => p.ProductID);
            productsInOrder = new SourceList<ProductInOrder>();
            employees = new SourceList<Employee>();
            customers = new SourceList<Customer>();

            this.WhenValueChanged(vm => vm.SelectedProduct).Subscribe(p => AddToOrder(p));
            var canUnSelectExecute = this.WhenAnyValue(x => x.SelectedProductInOrder).
                Select(selectedProductInOrder => selectedProductInOrder == null ? false : true);

            products.Connect().
                Transform(product => new ProductOnStore(product)).
                Filter(x => x.UnitsInStock != 0).
                Sort(SortExpressionComparer<ProductOnStore>.Ascending(item => item.ProductID)).
                ObserveOnDispatcher().
                Bind(out _productsInStore).
                Subscribe();

            productsInOrder.Connect().
                Sort(SortExpressionComparer<ProductInOrder>.Ascending(x => x.ProductID)).
                ObserveOnDispatcher().
                Bind(out _productsInOrder).
                ActOnEveryObject(x => SubscribeToChanges(x), y => SetInitialValues(y));

            employees.Connect().
                ObserveOnDispatcher().
                Bind(out _employees).
                Subscribe();

            customers.Connect().
                ObserveOnDispatcher().
                Bind(out _customers).
                Subscribe();

            UnSelectCommand = ReactiveCommand.Create(RemoveFromOrder, canUnSelectExecute);
        }

        #region Commands
        public ReactiveCommand<Unit, Unit> UnSelectCommand { get; }

        private void RemoveFromOrder() => productsInOrder.Remove(SelectedProductInOrder);
        #endregion

        #region Utilities
        private void AddToOrder(ProductOnStore product)
        {
            if (product == null) return;
            if (productsInOrder.Items.Any(o => o.ProductID == product.ProductID)) return;

            if (productsInOrder.Count == 0) OrderDate = DateTime.Now.ToLongDateString();

            ProductInOrder productInOrder = new ProductInOrder(product);
            productsInOrder.Add(productInOrder);
        }

        private void SetInitialValues(ProductInOrder productToRemove)
        {
            productToRemove.SelectedQuantity = 0;

            products.Edit((a) =>
            {
                var tempProduct = a.Items.FirstOrDefault(b => b.ProductID == productToRemove.ProductID);
                tempProduct.UnitsInStock += productToRemove.SourceProductOnStore.UnitsOnOrder;
                tempProduct.UnitsOnOrder = 0;
            });

            //productToRemove.SourceProductOnStore.UnitsInStock += productToRemove.SourceProductOnStore.UnitsOnOrder;
            //productToRemove.SourceProductOnStore.UnitsOnOrder = 0;

            TotalSum -= productToRemove.Sum;
        }

        private void SubscribeToChanges(ProductInOrder newProductInOrder)
        {
            newProductInOrder.WhenAnyValue(x => x.SelectedDiscount, x => x.SelectedQuantity)
            .Subscribe(a =>
            {
                int newSelectedDiscount = a.Item1;
                int newSelectedQuantity = a.Item2;

                //-UnitPrice или +UnitPrice к TotalSum и цене товара в заказке
                decimal newValue = (newSelectedQuantity - (short)newProductInOrder.SourceProductOnStore.UnitsOnOrder) * (decimal)newProductInOrder.UnitPrice;

                //-1% или +1% скидки от товара
                decimal percentageOff = (decimal)(newSelectedDiscount - newProductInOrder.PreviousSelectedDiscount) / 100;

                //Запускается когда меняется SelectedDiscount
                if (percentageOff != 0)
                {
                    newProductInOrder.Sum -= newProductInOrder.SelectedQuantity * (decimal)newProductInOrder.UnitPrice * percentageOff;
                    TotalSum -= newProductInOrder.SelectedQuantity * (decimal)newProductInOrder.UnitPrice * percentageOff;
                }
                //Запускается когда меняется SelectedQuantity и при этом SelectedDiscount больше нуля
                else if (newSelectedDiscount != 0)
                {
                    decimal sumOff = ((decimal)newProductInOrder.PreviousSelectedDiscount / 100) * newProductInOrder.SelectedQuantity * (decimal)newProductInOrder.UnitPrice;
                    decimal sumToAdd = sumOff - newProductInOrder.Sum;

                    newProductInOrder.Sum = sumOff;
                    TotalSum += sumToAdd;

                    return;
                }

                newProductInOrder.PreviousSelectedDiscount = newSelectedDiscount;

                newProductInOrder.Sum += newValue;
                TotalSum += newValue;
            });

            newProductInOrder.WhenAnyValue(x => x.SelectedQuantity)
                .Select(newSelectedQuantity =>
                {
                    short? unitsOnOrder = newProductInOrder.SourceProductOnStore.UnitsOnOrder;

                    return newSelectedQuantity - unitsOnOrder;
                })
                .Subscribe(newValue =>
                {
                    newProductInOrder.SourceProductOnStore.UnitsInStock -= (short)newValue;
                    newProductInOrder.SourceProductOnStore.UnitsOnOrder += (short)newValue;

                    SaveChangesToProducts(newProductInOrder, (int)newValue);
                });
        }

        private void SaveChangesToProducts(ProductInOrder newProductInOrder, int newValue)
        {
            Product productToReplace = northwindContext.Products.First(x => x.ProductID == newProductInOrder.ProductID);

            productToReplace.UnitsInStock -= (short)newValue;

            northwindContext.SaveChanges();
        }
        #endregion

        #region Implementation of INavigationAware
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (products.Count == 0) await Task.Run(() => products.AddOrUpdate(northwindContext.Products));
            if (employees.Count == 0) await Task.Run(() => employees.AddRange(northwindContext.Employees));
            if (customers.Count == 0) await Task.Run(() => customers.AddRange(northwindContext.Customers));
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) { return true; }

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        #endregion

        #region Properties
        public ReadOnlyObservableCollection<ProductInOrder> ProductsInOrder => _productsInOrder;
        public ReadOnlyObservableCollection<ProductOnStore> ProductsInStore => _productsInStore;
        public ReadOnlyObservableCollection<Employee> Employees => _employees;
        public ReadOnlyObservableCollection<Customer> Customers => _customers;


        [Reactive] public string OrderDate { set; get; }
        [Reactive] public ProductOnStore SelectedProduct { private get; set; }
        [Reactive] public ProductInOrder SelectedProductInOrder { private get; set; }

        decimal _totalsum;
        public decimal TotalSum
        {
            set { this.RaiseAndSetIfChanged(ref _totalsum, value); }
            get { return _totalsum; }
        }

        public bool KeepAlive => true;
        #endregion

        #region Screen objects
        public class ProductOnStore : AbstractNotifyPropertyChanged
        {
            public ProductOnStore(Product sourceProduct)
            {
                this.ProductID = sourceProduct.ProductID;
                this.ProductName = sourceProduct.ProductName;
                this.UnitPrice = sourceProduct.UnitPrice;
                this.UnitsInStock = sourceProduct.UnitsInStock;
                this.UnitsOnOrder = sourceProduct.UnitsOnOrder;
            }

            #region Properties
            public int ProductID { set; get; }

            public string ProductName { set; get; }

            public decimal? UnitPrice { set; get; }

            short? _unitsInStock;
            public short? UnitsInStock
            {
                set { SetAndRaise(ref _unitsInStock, value); }
                get { return _unitsInStock; }
            }

            short? _unitsOnOrder;
            public short? UnitsOnOrder
            {
                set { SetAndRaise(ref _unitsOnOrder, value); }
                get { return _unitsOnOrder; }
            }
            #endregion
        }

        public class ProductInOrder : ReactiveObject
        {
            public ProductInOrder(ProductOnStore product)
            {
                ProductID = product.ProductID;
                ProductName = product.ProductName;
                UnitPrice = product.UnitPrice;
                SelectedDiscount = 0;
                SelectedQuantity = 1;

                Quantity = new List<int>();
                Quantity.AddRange(Enumerable.Range(1, Convert.ToInt32(product.UnitsInStock)));
                Discount = new List<int>(100);
                Discount.AddRange(Enumerable.Range(0, 101));

                SourceProductOnStore = product;
            }

            #region Properties
            public int ProductID { set; get; }

            public string ProductName { set; get; }

            public decimal? UnitPrice { set; get; }

            public List<int> Quantity { get; }

            public List<int> Discount { get; }

            [Reactive]
            public int SelectedQuantity { set; get; }

            [Reactive]
            public int SelectedDiscount { set; get; }

            public decimal Sum { set; get; }

            public ProductOnStore SourceProductOnStore { private set; get; }

            public int PreviousSelectedDiscount { set; get; }
            #endregion
        }
        #endregion
    }
}
