using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{

    public Move Think(Board board, Timer timer)
    {
        return Move.NullMove;
    }

    static int[] PieceValue = {0, 100, 300, 300, 500, 900, 10000 };

    static int EvaluateBoard(Board board) {
        if (board.IsInCheckmate()) return 100;

        bool them = board.IsWhiteToMove, us = !them;
        int Score = 0;
        for(PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
            int balance = board.GetPieceList(type, us).Count - board.GetPieceList(type, them).Count;
            Score += PieceValue[(int)type] * balance;
        }

        return Score;
    }
}
