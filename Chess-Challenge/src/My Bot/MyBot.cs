using ChessChallenge.API;
using System;

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
    const uint TTSize = 2 ^ 20;
    static int[] PieceValue = { 0, 100, 300, 300, 500, 900, 10000 };
    const int Infinity = 1000000000, MateBaseScore = 100000000;

    Timer timer;
    Board board;
    Transposition[] tt = new Transposition[TTSize];
    Move bestRootMove = Move.NullMove;

    public Move Think(Board board, Timer timer)
    {
        this.timer = timer;
        this.board = board;

        int score, depth;
        for (score = 0, depth = 1; MayThink(); depth++)
            score = -Search(-Infinity, Infinity, depth, 0);
        Console.WriteLine("{0,10} @ {1} in {2}ms", score, depth,
            timer.MillisecondsElapsedThisTurn
        );

        return bestRootMove;
    }

    int Evaluate() {

        bool us = board.IsWhiteToMove, them = !us;
        int Score = 0;
        for (PieceType type = PieceType.Pawn; type < PieceType.King; type++) {
            int balance =
                board.GetPieceList(type, us).Count
                - board.GetPieceList(type, them).Count;
            Score += PieceValue[(int)type] * balance;
        }

        return Score;
    }

    bool MayThink() {
        return timer.MillisecondsElapsedThisTurn < MaxThinkTime;
    }

    int Search(int alpha, int beta, int depth, int fromRoot) {
        ref Transposition tr = ref tt[board.ZobristKey % TTSize];
        if (fromRoot > 0 && tr.IsCutoff(board, depth, alpha, beta))
            return tr.Score;

        if (depth == 0 || !MayThink())
            return Evaluate();

        var bestScore = -Infinity;
        var bestMove = Move.NullMove;
        var moves = board.GetLegalMoves();
        if (moves.Length == 0)
            return board.IsInCheckmate() ? fromRoot - MateBaseScore : 0;

        int[] mScores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++) {
            var move = moves[i];
            mScores[i] =
                move == tr.Move ? Infinity :
                move.IsCapture ? (100 * (int)move.CapturePieceType
                                  - (int)move.MovePieceType) :
                0;
        }

        ScoreType scoreType = ScoreType.UpperBound;
        for (int i = 0; i < mScores.Length; i++) {
            var iMax = i;
            for (int j = i + 1; j < mScores.Length; j++)
                if (mScores[iMax] < mScores[j]) iMax = j;
            if (iMax != i) {
                (mScores[i], mScores[iMax]) = (mScores[iMax], mScores[i]);
                (moves[i], moves[iMax]) = (moves[iMax], moves[i]);
            }

            var move = moves[i];
            board.MakeMove(move);
            var score = -Search(-beta, -alpha, depth - 1, fromRoot + 1);
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
                if (fromRoot == 0) bestRootMove = bestMove;
            }
        }
        return tr.Store(board, bestMove, depth, alpha, scoreType);
    }
}
