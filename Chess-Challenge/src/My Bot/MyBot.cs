using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    enum ScoreType { None, Exact, LowerBound, UpperBound }
    struct Transposition {
        public ScoreType ScoreType;
        public ulong ZobristKey;
        public int Depth;
        public int Score;
        public bool IsCutoff(Board board, int depth, int alpha, int beta) {
            return
                ZobristKey == board.ZobristKey
                && Depth >= depth
                && (
                    ScoreType == ScoreType.Exact
                    || ScoreType == ScoreType.LowerBound && Score >= beta
                    || ScoreType == ScoreType.UpperBound && Score <= alpha
                );
        }
    }

    int MaxThinkTime = 700;
    const uint TTSize = 2^20;
    static int[] PieceValue = {0, 100, 300, 300, 500, 900, 10000 };
    static int Infinity = 1000000000;

    Timer timer;
    Board board;
    Transposition[] tt = new Transposition[TTSize];

    public Move Think(Board board, Timer timer)
    {
        this.timer = timer;
        this.board = board;
        return SearchRoot();
    }

    int Evaluate() {
        if (board.IsInCheckmate()) return -Infinity;

        bool us = board.IsWhiteToMove, them = !us;
        int Score = 0;
        for(PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
            int balance = board.GetPieceList(type, us).Count - board.GetPieceList(type, them).Count;
            Score += PieceValue[(int)type] * balance;
        }

        return Score;
    }

    bool MayThink() {
        return timer.MillisecondsElapsedThisTurn < MaxThinkTime;
    }

    Move SearchRoot() {
        int bestScore = -Infinity;
        Move bestMove = Move.NullMove;

        var depth = 2;
        while(MayThink()) {
            depth++;
            foreach(var move in board.GetLegalMoves()) {
                board.MakeMove(move);
                var score = - Search(-Infinity, Infinity, depth);
                board.UndoMove(move);
                if (score >= bestScore) {
                    bestScore = score;
                    bestMove = move;
                }
                if (!MayThink()) break;
            }
        }
        Console.WriteLine(
            "{0,10} @ {1} in {2}ms",
            bestScore, depth, timer.MillisecondsElapsedThisTurn
        );
        return bestMove;
    }

    int Search(int alpha, int beta, int depth) {
        var alpha0 = alpha;

        ref Transposition tr = ref tt[board.ZobristKey % TTSize];
        if ( tr.IsCutoff(board, depth, alpha, beta) )
            return tr.Score;

        if (depth == 0 || !MayThink())
            return Evaluate();
        tr.ZobristKey = board.ZobristKey;
        tr.Depth = depth;
        foreach(var move in board.GetLegalMoves()) {
            board.MakeMove(move);
            var score = - Search( -beta, -alpha, depth - 1);
            board.UndoMove(move);
            if (score >= beta) {
                tr.ScoreType = ScoreType.LowerBound;
                return tr.Score = beta;
            }
            if (score > alpha) alpha = score;
        }
        tr.ScoreType = alpha > alpha0 ? ScoreType.Exact : ScoreType.UpperBound;
        return tr.Score = alpha;
    }
}
