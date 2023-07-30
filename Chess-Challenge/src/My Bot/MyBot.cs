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
        public Move move;
        public double score;
        Option[] futures = new Option[0];

        static double[] PieceValue = { 0, 1, 3, 3, 5, 9, 0};
        static Random rnd = new Random();

        public Option() { }

        public Option(Move move) {
            this.move = move;
        }

        public Option GetFuture(Board board) {
            try {
                return futures.First(option => board.ZobristKey == option.ZobristKey);
            } catch (InvalidOperationException e) {
                return new Option();
            }
        }

        public void LookAhead(Board board, int depth) {
            // doesn't hurt to assign this repeatedly
            ZobristKey = board.ZobristKey;

            if (depth == 0) {
                // done looking ahead, just evaluate this position
                score = Evaluate(board);
                return;
            }

            if (futures.Length == 0) {
                var moves = board.GetLegalMoves();
                if (moves.Length == 0) {
                    score = Evaluate(board);
                    return;
                }
                futures = moves.Select(move => new Option(move)).ToArray();
            }

            foreach (Option option in futures) {
                board.MakeMove(option.move);
                option.LookAhead(board, depth - 1);
                board.UndoMove(option.move);
            }
            score = - futures.Select(o => o.score).Max();
        }

        public Option ChooseFuture() {
            return futures.MaxBy(option => option.score);
        }

        // evaluate the board from our point of view when it is their move
        double Evaluate(Board board) {
            if (board.IsInCheckmate()) return 1000;

            bool them = board.IsWhiteToMove, us = !them;
            double score = rnd.NextDouble() - 0.5;
            for(PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
                double balance = board.GetPieceList(type, us).Count() - board.GetPieceList(type, them).Count();
                score += PieceValue[(int)type] * balance;
            }
            return score;
        }
    }

    // a single option that was chosen either by us or them
    Option present = new Option();

    public Move Think(Board board, Timer timer)
    {
        // build new or choose a foretold future
        present = present.GetFuture(board);
        present.LookAhead(board, 3);
        // chose the best future
        present = present.ChooseFuture();
        if (!board.GetLegalMoves().Contains(present.move)) {
            Log($"MyBot about to make an illegal {present.move}");
        }
        return present.move;
    }
}
