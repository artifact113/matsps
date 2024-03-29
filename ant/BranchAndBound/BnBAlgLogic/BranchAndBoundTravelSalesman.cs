﻿using System;
using System.Collections.Generic;
using System.Text;

using System.Timers;
using System.Threading;

using matsps.CommonData;
using matsps.BranchAndBound.BnBAlgData;
using matsps.Parameters;

namespace matsps.BranchAndBound.BnBAlgLogic
{
    /// <summary>
    /// Алгоритм расчета задачи коммивояжера с помощью метода Ветвей и Границ
    /// </summary>
    class BranchAndBoundTravelSalesman
    {
        #region Конструкторы
        public BranchAndBoundTravelSalesman()                                                               
        {
            tmrTimer = new System.Timers.Timer(10);                                      //инициализация таймера
            tmrTimer.Elapsed += new ElapsedEventHandler(tmrTimer_Elapsed); //подписка на событие таймера "Elapsed"
        } 
        public BranchAndBoundTravelSalesman(CitiesCollection cities)
            : this()                                                                                        
        {
            Cities = cities;
        }
        public BranchAndBoundTravelSalesman(CitiesCollection cities, BnBParameters param):this(cities)      
        {
            _parameters = param;
        }
        #endregion

        #region Поля
        /// <summary>
        /// поле параметров алгоритма ветвей и границ
        /// </summary>
        private BnBParameters _parameters;

        /// <summary>
        /// Первый родительский узел. Начало дерева.
        /// </summary>
        /// 
        ///         0 - это он
        ///        / \
        ///       *   *
        private BnBNode _greatParentNode;

        /// <summary>
        /// Лист с отладочными данными
        /// </summary>
        private List<string> _liInfo;

        /// <summary>
        /// Критерий веса для дерева (минимальный суммарный вес)
        /// </summary>
        private double _dCriterialWeight = double.PositiveInfinity;

        private static int _stCountDown = 0;
        private static int _strCountUp = 0;

        private long _lNodeCount = 0;

        /// <summary>
        /// Список готовых путей
        /// </summary>
        List<string> liPath;

        /// <summary>
        /// Поток вычислений
        /// </summary>
        private Thread t = null;

        /// <summary>
        /// Таймер вычислений
        /// </summary>
        private System.Timers.Timer tmrTimer;         //таймер
        #endregion

        #region Свойства
        /// <summary>
        /// Возвращает или задает Коллекцию городов
        /// </summary>
        public CitiesCollection Cities                  
        {
            set;
            get;
        }

        /// <summary>
        /// Лист в отладочной информацией (только для чтения)
        /// </summary>
        public List<string> ResultInfo                  
        {
            get {
                return _liInfo;
            }
        }
        #endregion

        #region Свойства результатов расчета
        /// <summary>
        /// Коллекция городов с лучшим маршрутом (только для чтения)
        /// </summary>
        public CitiesCollection BestPath                        
        {
            get            
            {
                // Разбиваем элемент пути на индексы
                string[] strEl = liPath[0].Split('|');
                string[] strPath = strEl[1].Trim().Split(_greatParentNode.Data.Path.CharSplit);

                CitiesCollection cities = new CitiesCollection();
                cities.MaxDistance = Cities.MaxDistance;
                for (int i = 0; i < strPath.Length; i++)
                {
                    int index = Convert.ToInt32(strPath[i]);
                    City newCity = new City(Cities[index].X, Cities[index].Y);
                    newCity.Index = Cities[index].Index;
                    cities.Add(newCity);
                }

                return cities;
            }
        }

        /// <summary>
        /// Список коллекций городов с лучшим маршрутом (только для чтения)
        /// </summary>
        public List<CitiesCollection> BestPathList              
        {
            get
            {
                List<CitiesCollection> liCities = new List<CitiesCollection>();
                for (int j = 0; j < liPath.Count; j++)
                {
                    // Разбиваем элемент пути на индексы
                    string[] strEl = liPath[0].Split('|');
                    string[] strPath = strEl[1].Trim().Split(_greatParentNode.Data.Path.CharSplit);

                    CitiesCollection cities = new CitiesCollection();
                    cities.MaxDistance = Cities.MaxDistance;
                    for (int i = 0; i < strPath.Length; i++)
                    {
                        int index = Convert.ToInt32(strPath[i]);
                        City newCity = new City(Cities[index].X, Cities[index].Y);
                        newCity.Index = Cities[index].Index;
                        cities.Add(newCity);
                    }

                    liCities.Add(cities);
                }
                return liCities;
            }
        }
        #endregion

        #region Методы (внутренние)
        /// <summary>
        /// Обход вниз
        /// </summary>
        /// <param name="parent">Текущий родительский узел</param>
        /// <param name="calledNode">Вызвавший узел</param>
        private void GoDown(BnBNode currentNode, BnBNode calledNode)                
        {
            bool _continue = true;
            while (_continue)
            {
                // Если текущий суммарный вес больше, чем Критериальный суммарный вес
                if (currentNode.Data.SummWeight > _dCriterialWeight)
                {
                    currentNode.Forbidden = true;
                    currentNode.Viewed = true;

                    // Запускаем функцию ОбходВверх(текущий_узел);
                    GoUp(currentNode, calledNode);
                    _continue = false;
                    break;
                }

                // Если матрица 2х2
                if (currentNode.Data.Length == 2)
                {
                    for (int i = 0; i < currentNode.Data.Length; i++)
                        for (int j = 0; j < currentNode.Data.Length; j++)
                            if (currentNode.Data.Distance[i, j].Value == 0)
                            {
                                int childI = currentNode.Data.VerIndexes[i];
                                int childJ = currentNode.Data.HorIndexes[j];

                                currentNode.Data.RecalcPath(childI, childJ);
                            }
                    _dCriterialWeight = currentNode.Data.SummWeight;
                    //currentNode.Data.LogMatrix();

                    currentNode.Viewed = true;
                    _strCountUp++;
                    // Запускаем функцию ОбходВверх(текущий_узел);
                    GoUp(currentNode, calledNode);
                    _continue = false;
                    break;
                }

                //currentNode.Data.LogMatrix();
                if (currentNode.Viewed == false)
                {
                    double dMaxPowValue = currentNode.Data.GetMaxPowValue();
                    for (int i = 0; i < currentNode.Data.Length; i++)
                        for (int j = 0; j < currentNode.Data.Length; j++)
                            if (currentNode.Data.Distance[i, j].Pow == dMaxPowValue)
                            {
                                // Пересечение с максимальным значением степени

                                int childI = currentNode.Data.VerIndexes[i];
                                int childJ = currentNode.Data.HorIndexes[j];

                                // Берется дуга i,j
                                BnBNodeData childDataFirst = new BnBNodeData(currentNode.Data, i, j);
                                BnBNode childNodeFirst = new BnBNode(currentNode, childDataFirst);
                                childDataFirst.RecalcPath(childI, childJ);               // добавляем дугу в маршрут и исключаем закольцованность
                                childDataFirst.ReductedMatrix();                         // приведение матрицы
                                childDataFirst.PowCalc();                                // подсчет степеней матрицы
                                //childDataFirst.LogMatrix();

                                // Не берется дуга i,j
                                BnBNodeData childDataSecond = new BnBNodeData(currentNode.Data);
                                BnBNode childNodeSecond = new BnBNode(currentNode, childDataSecond);
                                childDataSecond.RemoveLoopback(childI, childJ);
                                childDataSecond.RemoveLoopback(childJ, childI);
                                childDataSecond.ReductedMatrix();
                                childDataSecond.PowCalc();
                                //childDataSecond.LogMatrix();

                                currentNode.Nodes.Add(childNodeFirst);
                                currentNode.Nodes.Add(childNodeSecond);

                                // служебные данные
                                _lNodeCount += 2;       // количество узлов в дереве
                                _liInfo.Add(" ");
                                _liInfo.Add(String.Format( "№: {0:00000}\tParent:{1:00000}", childNodeFirst.Index, childNodeFirst.ParentNode.Index));
                                _liInfo.Add(String.Format("Размерность: {0:#}x{1:#}", childNodeFirst.Data.Length, childNodeFirst.Data.Length));
                                _liInfo.Add(String.Format("Длина пути: {0:000.00}", childNodeFirst.Data.SummWeight));
                                //_liInfo.AddRange( childNodeFirst.Data.ListInfo() );
                                _liInfo.Add(" ");
                                _liInfo.Add(String.Format("№: {0:00000}\tParent:{1:00000}", childNodeSecond.Index, childNodeSecond.ParentNode.Index));
                                _liInfo.Add(String.Format("Размерность: {0:#}x{1:#}", childNodeSecond.Data.Length, childNodeSecond.Data.Length));
                                _liInfo.Add(String.Format("Длина пути: {0:000.00}", childNodeSecond.Data.SummWeight));
                                //_liInfo.AddRange( childNodeSecond.Data.ListInfo() );

                                break;
                            }

                    // Находим минимальный суммарный вес у потомков
                    double minSummWeight = double.MaxValue;
                    for (int i = 0; i < currentNode.Nodes.Count; i++)
                        if (currentNode.Nodes[i].Data.SummWeight < minSummWeight)
                            minSummWeight = currentNode.Nodes[i].Data.SummWeight;
                    // Запускаем функцию ОбходВниз для первого дочернего элемента с минимальным суммарным весом
                    for (int i = 0; i < currentNode.Nodes.Count; i++)
                        if (currentNode.Nodes[i].Data.SummWeight == minSummWeight)
                        {
                            _stCountDown++;
                            //GoDown(currentNode.Nodes[i]);
                            currentNode = currentNode.Nodes[i];
                            continue;
                        }
                }
            }
            return;
        }

        /// <summary>
        /// Обход вверх
        /// </summary>
        private void GoUp(BnBNode currentNode, BnBNode calledNode)                  
        {
            while (currentNode != calledNode)
            {
                //получили родителя
                BnBNode parentNode = currentNode.ParentNode;
                if (parentNode == null)
                    return;
                BnBNode childNode = currentNode;
                for (int i = 0; i < parentNode.Nodes.Count; i++)
                {
                    if (parentNode.Nodes[i] != childNode)
                    {
                        if (parentNode.Nodes[i].Data.SummWeight <= _dCriterialWeight)
                        {
                            if (parentNode.Nodes[i].Viewed == false)
                            {
                                // Обход вниз для i-го ребенка
                                _stCountDown++;
                                currentNode.Viewed = true;
                                GoDown(parentNode.Nodes[i], currentNode);
                                return;
                            }
                        }
                        else
                        {
                            parentNode.Nodes[i].Forbidden = true;
                        }
                    }
                }
                currentNode.Viewed = true;
                // Продолжаем функцию ОбходВверх для родителя родителя.  (поднимаемся на уровень выше)
                currentNode = currentNode.ParentNode;
            }
            return;
        }

        /// <summary>
        /// Находит лучшие пути
        /// </summary>
        /// <param name="currentNode">Текущий узед</param>
        /// <param name="liPath">Накопленный путь ввиде списка</param>
        /// <returns></returns>
        private List<string> FindBest(BnBNode currentNode, List<string> liPath)     
        {
            if (currentNode.Data.Length == 2)
            {
                if( currentNode.Data.SummWeight <= _dCriterialWeight )
                    liPath.Add(currentNode.Index + " | " + currentNode.Data.Path.PiePath[0] + " | " + currentNode.Data.SummWeight);
                return liPath;
            }

            for (int i = 0; i < currentNode.Nodes.Count; i++)
                if (currentNode.Nodes[i].Forbidden == false)
                    FindBest(currentNode.Nodes[i], liPath);
            return liPath;
        }
        #endregion

        #region Методы (внешние)
        /// <summary>
        /// Запуска расчета в алгоритме Ветвей и Границ
        /// </summary>
        public void CalculateAsync()                         
        {
            if (t == null)
            {
                t = new Thread(Calculate);
                t.IsBackground = true;
                t.Start();
            }
        }

        private void Calculate()              
        {            
            BnBNodeData nData = new BnBNodeData(Cities);    // заносим начальные данные из коллекции городов

            tmrTimer.Start(); //запуск таймера
            _liInfo = new List<string>();

            nData.ReductedMatrix();                         // приводим матрице по столбцам и строкам
            nData.PowCalc();                                // подсчет степеней у нулевых элементов            
            nData.LogMatrix();                              // вывод в логи для отладки
            _greatParentNode = new BnBNode(null, nData);          // создаем первый родительский узел с данными

            // Обход
            GoDown(_greatParentNode, null);

            liPath = FindBest(_greatParentNode, new List<string>());    // получаем лучшие пути
            // Записываем лучшие пути в отладочные данные
            _liInfo.Add("------результат------");
            foreach (string lp in liPath)
                _liInfo.Add(lp);

            tmrTimer.Stop();           //остановка таймера

            // Вычисление завершено
            OnFinally(new EventArgs());
            //CitiesCollection pathCities = BestPath;
            //List<CitiesCollection> pathCitiesList = BestPathList;
        }
        #endregion
            
        #region Событие
        /// <summary>
        /// Генерируется при изменении прогресса выполнения алгоритма
        /// </summary>
        //public event ProgressChanged eventProgressChanged;
        // Член-события, делегат
        public event EventHandler<BnBAlgChangesEventArgs> eventProgressChanged;
        public event EventHandler<EventArgs> eventFinally;

        // Уведомляет подписанные на событие объекты
        protected virtual void OnProgressChanged(BnBAlgChangesEventArgs e)  
        {
            EventHandler<BnBAlgChangesEventArgs> tmp = eventProgressChanged;

            if (tmp != null)
                tmp(this, e);
        }
        protected virtual void OnFinally(EventArgs e)                       
        {
            EventHandler<EventArgs> tmp = eventFinally;

            if (tmp != null)
            {
                tmp(this, e);
                t.Abort();
                t = null;
            }
        }

        // Метод, вызывающий событие
        protected virtual void tmrTimer_Elapsed(object sender, EventArgs e) 
        {
            //Подсчитываем процент прогресса
            BnBAlgChangesEventArgs eee = new BnBAlgChangesEventArgs( _lNodeCount , "Расчет", " узлов");

            OnProgressChanged(eee);
        }
        #endregion
    }

    //          Аргументы событий изменения в алгоритме Ветвей и Границ   
    /// <summary>
    /// Аргументы событий изменения в алгоритме Ветвей и Границ
    /// </summary>
    internal class BnBAlgChangesEventArgs : EventArgs
    {
        private readonly double _dPercent;
        private readonly string _strStatus;
        private readonly string _strLabel;

        public BnBAlgChangesEventArgs(double percent, string status, string label)
        {
            _dPercent = percent;
            _strStatus = status;
            _strLabel = label;
        }

        /// <summary>
        /// Процент выполнения алгоритма
        /// </summary>
        public double Percent           
        { get { return _dPercent; } }

        /// <summary>
        /// Статус выполнения алгоритма (только для чтения)
        /// </summary>
        public string Status            
        {
            get
            {
                return _strStatus;
            }
        }

        /// <summary>
        /// Подпись к аргументу (только для чтения)
        /// </summary>
        public string Label             
        {
            get
            {
                return _strLabel;
            }
        }
    };
}
