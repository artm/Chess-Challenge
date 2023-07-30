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
        public double score;
        Option[] futures = new Option[0];
        bool IsInitial = true;

        static double[] PieceValue = { 0, 1, 3, 3, 5, 9, 0};
        static Random rnd = new Random();

        public Option() { }

        public Option(Board board, Move move) {
            this.move = move;
            board.MakeMove(move);
            this.ZobristKey = board.ZobristKey;
            this.score = Option.EvaluateBoard(board);
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
            return futures.MaxBy(option => option.score);
        }

        // evaluate the board from our point of view when it is their move
        static double EvaluateBoard(Board board) {
            if (board.IsInCheckmate()) return 100;

            bool them = board.IsWhiteToMove, us = !them;
            double score = rnd.NextDouble() - 0.5;
            for(PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
                double balance = board.GetPieceList(type, us).Count() - board.GetPieceList(type, them).Count();
                score += PieceValue[(int)type] * balance;
            }

            if (board.IsDraw()) score -= 5;

            return score;
        }

        public void Evaluate(Board board) {
            if (this.futures.Length == 0) return;
            foreach (Option option in futures) {
                board.MakeMove(option.move);
                option.Evaluate(board);
                board.UndoMove(option.move);
            }
            score = - futures.Select(o => o.score).Max();
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
        return present.move;
    }
}
