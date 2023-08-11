﻿using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    Board board;
    Timer timer;
    Move bestRootMove;
    const int WinScore = 100000, Inf = 1000000;
    int[] PieceValue = { 0, 100, 300, 300, 400, 500, 10000 };

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;
        for(int depth=1; MayThink(); depth++) Search(depth);
        return bestRootMove;
    }

    int Search(int depth, int dFromRoot = 0, int alpha = -Inf, int beta = Inf)
    {
        if (board.IsInCheckmate())
            return dFromRoot - WinScore;
        else if (depth <= 0)
            return Evaluate();

        var moves = board.GetLegalMoves();
        int[] moveScores = ScoreMoves(moves);
        Move bestMove = Move.NullMove;
        bool finished = true;
        for(int i=0; i<moves.Length; i++) {
            if (!MayThink()) {
                finished = false;
                break;
            }
            var move = FindNextMove(moves, moveScores, i);
            board.MakeMove(move);
            int score = - Search(depth - 1, dFromRoot + 1, - beta, - alpha);
            board.UndoMove(move);
            if (score > alpha) {
                alpha = score;
                bestMove = move;
            }
            if (alpha >= beta) break;
        }
        if (dFromRoot == 0 && finished)
            bestRootMove = bestMove;
        return alpha;
    }

    int Evaluate()
    {
        int score = 0;
        for(PieceType pt=PieceType.Pawn; pt<PieceType.King; pt++) {
            score += PieceValue[(int)pt] * (
                board.GetPieceList(pt, board.IsWhiteToMove).Count -
                board.GetPieceList(pt, !board.IsWhiteToMove).Count);
        }
        return score;
    }

    int[] ScoreMoves(Move[] moves)
    {
        var scores = new int[moves.Length];
        for(int i=0; i<moves.Length; i++) {
            if (moves[i].IsCapture)
                scores[i] = 100 * (int)moves[i].CapturePieceType - (int)moves[i].MovePieceType;
        }
        return scores;
    }

    Move FindNextMove(Move[] moves, int[] scores, int i)
    {
        var (_, iMax) = scores.Select((score, i) => (score, i)).Skip(i).Max();
        (scores[i], scores[iMax]) = (scores[iMax], scores[i]);
        (moves[i], moves[iMax]) = (moves[iMax], moves[i]);
        return moves[i];
    }

    bool MayThink()
    {
        return timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 60;
    }
}
