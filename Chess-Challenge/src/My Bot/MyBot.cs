using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    class Transposition {
        public ulong ZobristKey;
        public int Depth = -1;
        public int Score;
    }

    Dictionary<ulong, Transposition> Transpositions = new Dictionary<ulong, Transposition>(100000);
    int tableHits = 0, indexCollisions = 0;

    int MaxThinkTime = 700;
    static int[] PieceValue = {0, 100, 300, 300, 500, 900, 10000 };
    static int Infinity = 1000000000;

    Timer timer;
    Board board;

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
                if (score > bestScore) {
                    bestScore = score;
                    bestMove = move;
                }
                if (!MayThink()) break;
            }
        }
        Console.WriteLine(
            "{0,10} @ {1} in {2}ms, {3} hits, {4} index collisions",
            bestScore, depth, timer.MillisecondsElapsedThisTurn, tableHits,
            indexCollisions
        );
        tableHits = 0;
        return bestMove;
    }

    int Search(int alpha, int beta, int depth) {
        Transposition tr;
        if (Transpositions.ContainsKey(board.ZobristKey)) {
            tr = Transpositions[board.ZobristKey];
            if (tr.ZobristKey != board.ZobristKey) {
                indexCollisions++;
                tr.ZobristKey = board.ZobristKey;
            } else if (tr.Depth >= depth) {
                tableHits++;
                return tr.Score;
            }
        } else {
            Transpositions[board.ZobristKey] = tr = new Transposition();
            tr.ZobristKey = board.ZobristKey;
        }
        tr.Depth = depth;
        if (depth == 0 || !MayThink()) return (tr.Score = Evaluate());
        foreach(var move in board.GetLegalMoves()) {
            board.MakeMove(move);
            var score = - Search( -beta, -alpha, depth - 1);
            board.UndoMove(move);
            if (score >= beta) return (tr.Score = beta);
            if (score > alpha) alpha = score;
        }
        return (tr.Score = alpha);
    }
}
