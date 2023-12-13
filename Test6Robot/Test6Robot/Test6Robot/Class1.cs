using Robot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test6Robot;

namespace Test6Robot
{

    public class ChaikovskyiAlgorithm : IRobotAlgorithm
    {
        public string Author => "Chaikovskyi Sviatoslav";
        public int radiusCollect = 1;
        public const int LIMIT_ENERGY_MOVE_NEW_STATION = 100;
        public const int MIN_ENERGY_ON_NEW_STATION = 750;
        public const int MAX_ENERGY_SPEND_MOVE = 450;
        public const int COUNT_ENERGY_TO_CREATE_NEW_ROBOT1 = 350;
        public const int NEW_ROBOT_ENERGY1 = 100;
        public const int COUNT_ENERGY_TO_CREATE_NEW_ROBOT2 = 650;
        public const int NEW_ROBOT_ENERGY2 = 400;
        public const int COUNT_ROBOT_LIMIT = 80;
        private int numMyRobots = 10;

        public RobotCommand DoStep(IList<Robot.Common.Robot> robots, int robotToMoveIndex, Map map)
        {

            Robot.Common.Robot movingRobot = robots[robotToMoveIndex];

            if (map.GetResource(movingRobot.Position) != null)
            {
                return HandleRobotOnStation(robots, movingRobot, map);
            }

            return HandleRobotNotOnStation(robots, movingRobot, map);
        }

        private RobotCommand HandleRobotOnStation(IList<Robot.Common.Robot> robots, Robot.Common.Robot movingRobot, Map map)
        {
            var countMyRobots = numMyRobots;

            if (countMyRobots < 100)
            {
                var command = CreateNewRobotIfPossible(movingRobot, countMyRobots);

                if (command != null)
                {
                    return command;
                }

                return HandleRobotOnStationWithEqualStations2(movingRobot, map, robots);
            }

            return HandleRobotOnStationWithEqualStations2(movingRobot, map, robots);
        }

        private RobotCommand HandleRobotOnStationWithEqualStations2(Robot.Common.Robot movingRobot, Map map, IList<Robot.Common.Robot> robots)
        {
            var myStation = map.GetResource(movingRobot.Position);

            if (myStation != null && myStation.Energy < LIMIT_ENERGY_MOVE_NEW_STATION)
            {
                return HandleRobotNotOnStation(robots, movingRobot, map);
            }

            return new CollectEnergyCommand();
        }

        private RobotCommand CreateNewRobotIfPossible(Robot.Common.Robot movingRobot, int countMyRobots)
        {
            if (countMyRobots <= COUNT_ROBOT_LIMIT && movingRobot.Energy > COUNT_ENERGY_TO_CREATE_NEW_ROBOT1)
            {
                numMyRobots++;
                return new CreateNewRobotCommand() { NewRobotEnergy = NEW_ROBOT_ENERGY1 };
            }

            if (movingRobot.Energy > COUNT_ENERGY_TO_CREATE_NEW_ROBOT2)
            {
                numMyRobots++;
                return new CreateNewRobotCommand() { NewRobotEnergy = NEW_ROBOT_ENERGY2 };
            }

            return null;
        }

        private RobotCommand HandleRobotNotOnStation(IList<Robot.Common.Robot> robots, Robot.Common.Robot movingRobot, Map map)
        {
            Position stationEnemyPosition = FindNearestEnemyStation2(movingRobot, map, robots);
            Position stationFreePosition = FindNearestFreeStation2(movingRobot, map, robots);

            if (stationFreePosition == null && stationEnemyPosition == null)
            {
                return new CollectEnergyCommand();
            }

            return MoveToStation(movingRobot, stationFreePosition, stationEnemyPosition, map);
        }

        private RobotCommand MoveToStation(Robot.Common.Robot movingRobot, Position stationFreePosition, Position stationEnemyPosition, Map map)
        {
            Robot.Common.Position newPosition;
            int lostEnergyFreeStation = DistanceHelper.FindDistance(movingRobot.Position, stationFreePosition);
            if (stationEnemyPosition != stationFreePosition)
            {
                int lostEnergyEnemyStation = DistanceHelper.FindDistance(movingRobot.Position, stationEnemyPosition);

                if (lostEnergyFreeStation <= lostEnergyEnemyStation)
                {
                    if (lostEnergyFreeStation < movingRobot.Energy)
                    {
                        return new MoveCommand() { NewPosition = stationFreePosition };
                    }

                    newPosition = StepToStation(movingRobot.Position, stationFreePosition, movingRobot.Energy - 10, map);

                    if (newPosition != null)
                    {
                        return new MoveCommand() { NewPosition = newPosition };
                    }
                            
                    return new CollectEnergyCommand();
                }
               
                if (lostEnergyEnemyStation < movingRobot.Energy - 50)
                {
                    return new MoveCommand() { NewPosition = stationEnemyPosition };
                }

                newPosition = StepToStation(movingRobot.Position, stationEnemyPosition, movingRobot.Energy - 30, map);

                if (newPosition != null)
                {
                    return new MoveCommand() { NewPosition = newPosition };
                }
                            
                return new CollectEnergyCommand();
            }


            if (lostEnergyFreeStation < movingRobot.Energy)
            {
                return new MoveCommand() { NewPosition = stationFreePosition };
            }

            newPosition = StepToStation(movingRobot.Position, stationFreePosition, movingRobot.Energy - 10, map);
                
            if (newPosition != null)
            {
               return new MoveCommand() { NewPosition = newPosition };
            }

            return new CollectEnergyCommand();
        }

        public Position StepToStation(Position a, Position b, int energy, Map map)
        {
            Position newPosition;
            int lostEnergy = DistanceHelper.FindDistance(a, b);
            double result = (double)lostEnergy / energy;
            int roundedResult = (int)Math.Ceiling(result);
            int newX = a.X + ((int)Math.Floor((double)(b.X - a.X) / roundedResult));
            int newY = a.Y + ((int)Math.Floor((double)(b.Y - a.Y) / roundedResult));

            newPosition = new Position(newX, newY);

            if (map.IsValid(newPosition))
            {
                return newPosition;
            }

            return null;
        }

        public Position FindNearestFreeStation2(Robot.Common.Robot movingRobot, Map map, IList<Robot.Common.Robot> robots)
        {
            EnergyStation nearest = null;
            int minDistance = int.MaxValue;
            int maxProfit = int.MinValue;
            int thisProfit = GetEnergyProfit(movingRobot.Position, 0, map);

            foreach (var station in map.Stations)
            {
                if (station != null && IsStationFree(station, movingRobot, robots))
                {
                    int d = DistanceHelper.FindDistance(station.Position, movingRobot.Position);
                    int energyProfit = GetEnergyProfit(station.Position, d, map);

                    if (energyProfit > thisProfit && energyProfit > maxProfit && d < minDistance)
                    {
                        maxProfit = energyProfit;
                        nearest = station;
                        minDistance = d;
                    }

                }
            }

            return nearest == null ? null : nearest.Position;
        }

        public int GetEnergyProfit(Position newPosition, int d, Map map)
        {
            int sum = 0;

            foreach (var station in map.GetNearbyResources(newPosition, 1))
            {
                sum += station.Energy;
            }

            return sum - d;
        }


        public Position FindNearestEnemyStation2(Robot.Common.Robot movingRobot, Map map,
        IList<Robot.Common.Robot> robots)
        {
            EnergyStation nearest = null;
            int minDistance = int.MaxValue;
            int maxProfit = int.MinValue;
            int thisProfit = GetEnergyProfit(movingRobot.Position, 0, map);

            foreach (var station in map.Stations)
            {
                if (IsStationFreeFromMyRobots(station, movingRobot, robots))
                {
                    int d = DistanceHelper.FindDistance(station.Position, movingRobot.Position);
                    int energyProfit = GetEnergyProfit(station.Position, d, map) - 50;

                    if (energyProfit > thisProfit && energyProfit > maxProfit && d < minDistance)
                    {
                        maxProfit = energyProfit;
                        nearest = station;
                        minDistance = d;
                    }
                }
            }

            return nearest == null ? null : nearest.Position;
        }

        public bool IsStationFree(EnergyStation station, Robot.Common.Robot movingRobot, IList<Robot.Common.Robot> robots)
            => IsCellFree(station.Position, movingRobot, robots);

        public bool IsStationFreeFromMyRobots(EnergyStation station, Robot.Common.Robot movingRobot, IList<Robot.Common.Robot> robots) 
            => IsFreeCell(station.Position, movingRobot, robots);

        public bool IsCellFree(Position cell, Robot.Common.Robot movingRobot, IList<Robot.Common.Robot> robots)
            => !robots.Any(robot => robot.Position == cell);

        public bool IsFreeCell(Position cell, Robot.Common.Robot movingRobot, IList<Robot.Common.Robot> robots)
            => !robots.Any(robot => robot.Position == cell && robot.OwnerName != Author);

    }
}
