using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
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
        var ahead = SeeAhead(board);
        return ahead.MaxBy(option => option.score).move;
    }

    Option[] SeeAhead(Board board) {
        var moves = board.GetLegalMoves();
        var options = moves.Select(move => new Option(move)).ToArray();
        foreach (Option option in options) {
            board.MakeMove(option.move);
            option.score = - Evaluate(board);
            board.UndoMove(option.move);
        }
        return options;
    }

    // evaluate the board from point of view of whose move it is
    int Evaluate(Board board) {
        if (board.IsInCheckmate()) return -1000;

        bool us = board.IsWhiteToMove, them = !us;
        int score = 0;
        for(PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
            int balance = board.GetPieceList(type, us).Count() - board.GetPieceList(type, them).Count();
            score += PieceValue[(int)type] * balance;
        }
        return score;
    }
}
