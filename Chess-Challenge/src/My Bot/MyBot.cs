using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

using static ChessChallenge.Application.ConsoleHelper;

public class MyBot : IChessBot
{
    class Option {
        // board hash if the option becomes reality
        ulong ZobristKey;
        public Move move = Move.NullMove;
        public double Score;
        public int Depth = 1;
        Option[] futures = new Option[0];
        bool IsInitial = true;

        static double[] PieceValue = { 0, 1, 3, 3, 5, 9, 0};
        static Random rnd = new Random();

        public Option() { }

        public Option(Board board, Move move) {
            this.move = move;
            board.MakeMove(move);
            this.ZobristKey = board.ZobristKey;
            this.Score = Option.EvaluateBoard(board);
            var moves = board.GetLegalMoves();
            board.UndoMove(move);
        }

        public Option GetFuture(Board board) {
            try {
                return futures.First(option => board.ZobristKey == option.ZobristKey);
            } catch (InvalidOperationException e) {
                return new Option();
            }
        }

        public void LookAhead(Board board, Timer timer, int digTime) {
            while( digTime > timer.MillisecondsElapsedThisTurn ){
                LookFurtherAhead(board, timer, digTime);
            }
        }

        void LookFurtherAhead(Board board, Timer timer, int digTime) {
            if (this.IsInitial && digTime > timer.MillisecondsElapsedThisTurn) {
                // try to grow further
                var moves = board.GetLegalMoves();
                futures = moves.Select(move => new Option(board, move)).ToArray();
                this.IsInitial = false;
            } else {
                // recurse
                foreach (Option option in futures) {
                    board.MakeMove(option.move);
                    option.LookFurtherAhead(board, timer, digTime);
                    board.UndoMove(option.move);
                }
            }
        }

        public Option ChooseFuture() {
            //return futures.MaxBy(option => option.Score);
            var maxScore = futures.Select(o => o.Score).Max();
            var bestOptions = futures.Where(o => o.Score == maxScore).ToArray();
            Log(String.Format("Selecting from {0} options with score {1,6:F2}", bestOptions.Length, maxScore));
            var i = rnd.Next(0, bestOptions.Length);
            return bestOptions[i];
        }

        // evaluate the board from our point of view when it is their move
        static double EvaluateBoard(Board board) {
            if (board.IsInCheckmate()) return 100;

            bool them = board.IsWhiteToMove, us = !them;
            double Score = 0;
            for(PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
                double balance = board.GetPieceList(type, us).Count() - board.GetPieceList(type, them).Count();
                Score += PieceValue[(int)type] * balance;
            }

            return Score;
        }

        public void Evaluate(Board board) {
            if (this.futures.Length == 0) return;
            foreach (Option option in futures) {
                board.MakeMove(option.move);
                option.Evaluate(board);
                board.UndoMove(option.move);
            }
            var worst = futures.OrderByDescending(o => (o.Score, - o.Depth)).First();
            Score = - worst.Score;
            Depth = worst.Depth + 1;
        }
    }

    // a single option that was chosen either by us or them
    Option present = new Option();

    public Move Think(Board board, Timer timer)
    {
        // build new or choose a foretold future
        present = present.GetFuture(board);
        present.LookAhead(board, timer, 750);
        present.Evaluate(board);
        // chose the best future
        present = present.ChooseFuture();
        Log(String.Format("Score: {0,6:F2} @ {1}", present.Score, present.Depth));
        return present.move;
    }
}
