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
                var score = - Search(-Infinity, Infinity, depth, 0);
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

    int Search(int alpha, int beta, int depth, int fromRoot) {
        ref Transposition tr = ref tt[board.ZobristKey % TTSize];
        if ( tr.IsCutoff(board, depth, alpha, beta) )
            return tr.Score;

        if (depth == 0 || !MayThink())
            return Evaluate();

        var bestScore = -Infinity-1;
        var bestMove = Move.NullMove;
        var moves = board.GetLegalMoves();


        int[] mScores = new int[moves.Length];
        for(int i=0; i<moves.Length; i++) {
            var move = moves[i];
            mScores[i] =
                move == tr.Move ? Infinity :
                move.IsCapture ? (100 * (int)move.CapturePieceType - (int)move.MovePieceType) :
                0;
        }

        ScoreType scoreType = ScoreType.UpperBound;
        for(int i=0; i<mScores.Length; i++) {
            var iMax = i;
            for(int j=i+1; j<mScores.Length; j++)
                if (mScores[iMax] < mScores[j]) iMax = j;
            if (iMax != i) {
                (mScores[i], mScores[iMax]) = (mScores[iMax], mScores[i]);
                (moves[i], moves[iMax]) = (moves[iMax], moves[i]);
            }

            var move = moves[i];
            board.MakeMove(move);
            var score = - Search( -beta, -alpha, depth - 1, fromRoot + 1);
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
