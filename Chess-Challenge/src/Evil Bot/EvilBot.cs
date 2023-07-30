using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example
{

    // see 4 plys ahead and look for checkmate / captures of the most expensive
    // pieces
    public class EvilBot : IChessBot
    {
        class Option {
            public Move move;
            public int score;

            public Option(Move move) {
                this.move = move;
                score = 0;
            }
        }

        int[] PieceValue = { 0, 1, 3, 3, 5, 9, 0};

        public Move Think(Board board, Timer timer)
        {
            var ahead = SeeAhead(board, 4);
            return ahead.MaxBy(option => option.score).move;
        }

        Option[] SeeAhead(Board board, int depth) {
            var moves = board.GetLegalMoves();
            var options = moves.Select(move => new Option(move)).ToArray();
            foreach (Option option in options) {
                board.MakeMove(option.move);
                if (depth == 1) {
                    option.score = Evaluate(board);
                } else {
                    var theirOptions = SeeAhead(board, depth - 1);
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
        int Evaluate(Board board) {
            if (board.IsInCheckmate()) return 1000;

            bool them = board.IsWhiteToMove, us = !them;
            int score = 0;
            for(PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
                int balance = board.GetPieceList(type, us).Count() - board.GetPieceList(type, them).Count();
                score += PieceValue[(int)type] * balance;
            }
            return score;
        }
    }

}
