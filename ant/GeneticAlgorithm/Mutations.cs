﻿using System;
using System.Collections.Generic;
using System.Text;

using matsps.CommonData;

namespace matsps.GeneticAlgorithm
{
    class Mutations
    {
        #region Constuctors

        public Mutations()
        {
            //this.SetMutationsProbability(1, 1, 1);
        }

        public Mutations(int iCitySwitchMutation, int iIsolatedChainMutation, int iNewAgentMutation)
        {
            this.SetMutationsProbability(iCitySwitchMutation, iIsolatedChainMutation, iNewAgentMutation);
        }

        #endregion Constuctors

        #region Variables

       // private GAParameters _gap;
        
        private static Random _rnd = new Random();

        private double _dCitySwitchMutation;
        private double _dIsolatedChainMutation;
        private double _dNewAgentMutation;
        #endregion Variables

        #region Functions

        /// <summary>
        /// Probability in percent or in number, ex:(1,1,1) means equal probability
        /// </summary>
        /// <param name="iCitySwitchMutation">Probability of City Switch Mutation</param>
        /// <param name="iIsolatedChainMutation">Probability of Isolated Chain Mutation</param>
        /// <param name="iNewAgentMutation">Probability of New Agent Mutation</param>
        public void SetMutationsProbability(int iCitySwitchMutation, int iIsolatedChainMutation, int iNewAgentMutation)
        {
            int iSumm;
            iSumm = iCitySwitchMutation + iIsolatedChainMutation + iNewAgentMutation;

            _dCitySwitchMutation = (double)iCitySwitchMutation / (double)iSumm;
            _dIsolatedChainMutation = (double)iIsolatedChainMutation / (double)iSumm;
            _dNewAgentMutation = (double)iIsolatedChainMutation / (double)iSumm;

        }

        /// <summary>
        /// Perform a mutation of single Agent
        /// </summary>
        /// <param name="agent">Agent to mutate(must contain a CitiesCollection!)</param>
        public void Perform(ref Agent agent)
        {
            double dProbability = _rnd.NextDouble();

            if (dProbability < _dCitySwitchMutation)
            {
                this.CitySwitchMutation(ref agent);
            }
            else if (dProbability < _dCitySwitchMutation + _dIsolatedChainMutation)
            {
                this.IsolatedChainMutation(ref agent);
            }
            else //if(dProbability < _dCitySwitchMutation + _dIsolatedChainMutation + _dNewAgentMutation)
            {
                this.NewAgentMutation(ref agent);
            }
        }

        public void CitySwitchMutation(ref Agent agent)
        {
            // Getting Cities collection
            CitiesCollection tmpCC = new CitiesCollection(agent.Route.Cities);

            // Choosing random cities indexes
            int iCity1, iCity2;

            iCity1 = _rnd.Next(tmpCC.Count);
            iCity2 = _rnd.Next(tmpCC.Count);

            // Writing cities in local variables
            City tmpc1 = tmpCC[iCity1];
            City tmpc2 = tmpCC[iCity2];

            // Switching cities
            tmpCC[iCity1] = tmpc2;
            tmpCC[iCity2] = tmpc1;

            // Adding temporary collection to the Agent
            agent.Route = new Route(tmpCC, "генетический алгоритм");

        }

        public void IsolatedChainMutation(ref Agent agent)
        {
            // Mutation takes place only if it is more than 4 cities in collection
            if (agent.Route.Cities.Count > 4)
            {
                // Getting Cities collection
                CitiesCollection tmpCC = new CitiesCollection(agent.Route.Cities);

                // Choosing random cities indexes
                int iCity1, iCity2;

                // Lower bound
                iCity1 = _rnd.Next(tmpCC.Count - 4);
                // Upper bound
                iCity2 = _rnd.Next(iCity1 + 4, tmpCC.Count);

                List<City> _liCityTemp = new List<City>();
                for (int i = iCity1 + 1; i < iCity2; i++)
                {
                    _liCityTemp.Add(tmpCC[i]);
                }

                for (int i = iCity1 + 1; i < iCity2; i++)
                {
                    int iRnd = _rnd.Next(_liCityTemp.Count);
                    tmpCC[i] = _liCityTemp[iRnd];
                    _liCityTemp.RemoveAt(iRnd);

                }

                agent.Route = new Route(tmpCC, "генетический алгоритм");

                if (Generation.IsCorruptedRoute(ref agent))
                {

                }
            }
            
        }

        public void NewAgentMutation(ref Agent agent)
        {
            agent = new Agent(agent.Route.Cities);

        }

        #endregion Functions
    }
}
