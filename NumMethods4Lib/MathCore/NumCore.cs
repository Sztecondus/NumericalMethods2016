﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NumMethods4Lib.MathCore
{
    public enum IntervalTypes
    {
        BothOpen,
        BothClosed,
        LeftOpen, //right closed
        RightOpen,
        InfLeftRightOpen,
        InfLeftRightClosed,
        InfRightLeftOpen,
        InfRightLeftClosed,
        InfBoth,
    }

    public struct Point
    {
        public double X;
        public double Y;
    }

    public static class NumCore
    {
        public static double NewtonikCortesik(double fromX, double toX,IFunction selecetedFunction, double acc,int maxIter,IntervalTypes type = IntervalTypes.BothClosed)
        {
            int intervals = 1,iter = 0;
            var nodes = new List<Point> {new Point {X = fromX,Y=selecetedFunction.GetValue(fromX)} };
            double sum1=0, sum2;
            do
            {
                var nodeCount = 2 * intervals + 1;
                switch (type)
                {
                    case IntervalTypes.BothOpen:
                        var dif = (toX - fromX) / (2 * intervals);
                        fromX += dif;
                        toX -= dif;
                        break;
                    case IntervalTypes.BothClosed:
                        break;
                    case IntervalTypes.LeftOpen:
                        fromX += (toX - fromX) / (2 * intervals);
                        break;
                    case IntervalTypes.RightOpen:
                        toX -= (toX - fromX) / (2 * intervals);
                        break;
                    default:
                        return InfiniteNewtonikCortesik(fromX, toX, selecetedFunction, maxIter, type);
                }
                var xDiff = (toX - fromX)/(2 * intervals);
                nodes.DoNodes(xDiff,nodeCount,selecetedFunction);
                sum2 = sum1;
                sum1 = nodes.Skip(1).Take(nodeCount - 1).Select((point, i) => ((i + 1)%2 == 1 ? 4 : 2)*point.Y).Sum() * xDiff / 3;
                intervals *= 2;
                iter++;
            } while (Math.Abs(sum1 - sum2) > acc && iter++ < maxIter);
            if(iter == maxIter)
                throw new IndexOutOfRangeException("Trolorlo za dużo itercajów");
            return sum1;
        }

        private static double InfiniteNewtonikCortesik(double fromX, double toX, IFunction selecetedFunction, int maxIter, IntervalTypes type)
        {
            int intervals = 1, iter = 0;
            double node = fromX;
            switch (type)
            {
                case IntervalTypes.InfLeftRightClosed:
                case IntervalTypes.InfRightLeftClosed:
                    break;
                case IntervalTypes.InfRightLeftOpen:
                    fromX += (toX - fromX) / (2 * intervals);
                    node = fromX;
                    break;
                case IntervalTypes.InfLeftRightOpen:
                    toX -= (toX - fromX) / (2 * intervals);
                    node = toX;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Muri!　無 ");
            }
            double sum1 = 0;
            double delta = .5f;

            
            while (iter++ < maxIter)
            {
                sum1 += selecetedFunction.GetValue(node)*delta/3;
                if (type == IntervalTypes.InfLeftRightOpen || type == IntervalTypes.InfLeftRightClosed)
                    node -= delta;
                else
                    node += delta;
            }
            if (iter == maxIter)
                throw new IndexOutOfRangeException("Trolorlo za dużo itercajów");
            return sum1;
        }
        /// <summary>
        /// Source: http://mathworld.wolfram.com/Laguerre-GaussQuadrature.html
        /// </summary>
        private static readonly List<Tuple<double, double>> LaguerreNodes = new List<Tuple<double, double>>
        {
            new Tuple<double, double>(0.26356, 0.521756), //root , weight
            new Tuple<double, double>(1.4134, 0.398667),
            new Tuple<double, double>(3.59643, 0.0759424),
            new Tuple<double, double>(7.08581, 0.00361176),
            new Tuple<double, double>(12.6408, 0.00002337),
        };

        public static double LaguerreIntegration(IFunction fun,int n)
        {
            return LaguerreNodes.Take(n).Sum(node => node.Item2*fun.GetValue(node.Item1));
        }

        private static void DoNodes(this List<Point> list, double xDiff, int target, IFunction fun)
        {
            for (int i = 1; i < target; i += 2)
            {
                var curr = list[i - 1].X + xDiff;
                list.Insert(i, new Point {X = curr, Y = fun.GetValue(curr)});
            }

            if (target == 3) //init whatnot
                list.Add(new Point {X = list.Last().X + xDiff, Y = fun.GetValue(list.Last().X + xDiff)});
        }
        
    }
}