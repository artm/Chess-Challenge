using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    class Option {
        public Move move;
        public double score;
        Option[] options;

        public Option(Move move) {
            this.move = move;
        }
    }

    double[] PieceValue = { 0, 1, 3, 3, 5, 9, 0};
    Random rnd = new Random();

    public Move Think(Board board, Timer timer)
    {
        var ahead = LookAhead(board, 4);
        return ahead.MaxBy(option => option.score).move;
    }

    Option[] LookAhead(Board board, int depth) {
        var moves = board.GetLegalMoves();
        var options = moves.Select(move => new Option(move)).ToArray();
        foreach (Option option in options) {
            board.MakeMove(option.move);
            if (depth == 1) {
                option.score = Evaluate(board);
            } else {
                var theirOptions = LookAhead(board, depth - 1);
                if (theirOptions.Length > 0) {
                    // score for us is the inverse of their best move
                    option.score = - theirOptions.Select(o => o.score).Max();
                } else {
                    option.score = Evaluate(board);
                }
            }
            board.UndoMove(option.move);
        }
        return options;
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
