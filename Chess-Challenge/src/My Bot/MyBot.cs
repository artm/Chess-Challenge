using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    enum ScoreType { None, Exact, LowerBound, UpperBound }
    struct Transposition {
        public ulong ZobristKey;
        public Move Move;
        public int Depth;
        public int Score;
        public ScoreType ScoreType;
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
        public int Store(Board board, Move move, int depth, int score, ScoreType scoreType) {
            ZobristKey = board.ZobristKey;
            Move = move;
            Depth = depth;
            Score = score;
            ScoreType = scoreType;
            return score; // so it can be returned easily
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
        int bestScore = -Infinity-1;
        Move bestMove = Move.NullMove;

        var depth = 1;
        while(MayThink()) {
            depth++;
            foreach(var move in board.GetLegalMoves()) {
                board.MakeMove(move);
                var score = - Search(-Infinity, Infinity, depth);
                board.UndoMove(move);
                if (score > bestScore) {
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
        ref Transposition tr = ref tt[board.ZobristKey % TTSize];
        if ( tr.IsCutoff(board, depth, alpha, beta) )
            return tr.Score;

        if (depth == 0 || !MayThink())
            return Evaluate();

        var bestScore = -Infinity-1;
        var bestMove = Move.NullMove;

        ScoreType scoreType = ScoreType.UpperBound;

        var trMove = tr.Move;
        var moveScores = board.GetLegalMoves().Select(move => (move,
            move == trMove ? Infinity : 0
        )).ToArray();
        for(int i=0; i<moveScores.Count(); i++) {
            for(int j=i+1; j<moveScores.Count(); j++) {
                if (moveScores[i].Item2 < moveScores[j].Item2) {
                    (moveScores[i], moveScores[j]) = (moveScores[j], moveScores[i]);
                }
            }
            var move = moveScores[i].Item1;
            board.MakeMove(move);
            var score = - Search( -beta, -alpha, depth - 1);
            board.UndoMove(move);
            if (score >= beta)
                return tr.Store(board, move, depth, beta, ScoreType.LowerBound);
            if (score > alpha) {
                alpha = score;
                scoreType = ScoreType.Exact;
            }
            if (score > bestScore) {
                bestScore = score;
                bestMove = move;
            }
        }
        return tr.Store(board, bestMove, depth, alpha, scoreType);
    }
}
