using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    struct Position {
        public ulong Key;
        public Move Move;
        // -1 = lower, 0 = exact, 1 = upper
        public int BoundType, Score, Depth;
    }

    class OutOfTime : System.Exception {}

    Board board;
    Timer timer;
    Move bestRootMove;
    const int TTSize = 2^20;
    int[] PieceValue = { 0, 100, 300, 300, 500, 900, 10000 };
    Position[] Transpositions = new Position[TTSize];
    int timeBudget, selDepth;

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;
        bestRootMove = Move.NullMove;
        timeBudget = timer.MillisecondsRemaining / 30;
        int spentTimeBudget = 0, lastIterationTime = 0; // #DEBUG
        int alpha = -1000000, beta = 1000000;
        for(int depth = 1; MayThink();)
            try {
                selDepth = 0;
                var score = Search(depth, 0, alpha, beta);
                if (score <= alpha || score >= beta) {
                    alpha = -1000000;
                    beta = 1000000;
                    Console.WriteLine("[us] retry");
                    continue;
                }
                alpha = score - 50;
                beta = score + 50;
                lastIterationTime = timer.MillisecondsElapsedThisTurn - spentTimeBudget; // #DEBUG
                spentTimeBudget += lastIterationTime; // #DEBUG
                Console.WriteLine($"[us] depth {depth} selDepth {selDepth,2} score {score,-10} time {lastIterationTime,5} {bestRootMove}" ); // #DEBUG
                depth++;
            } catch (OutOfTime) {
                // it's ok, we've got the best move from the previous iteration
            }
        return bestRootMove;
    }

    int Search(int depth, int dFromRoot, int alpha, int beta, int maxExt = 5)
    {
        selDepth = Math.Max(selDepth, dFromRoot);

        if (board.IsInCheckmate())
            return dFromRoot - 100000;
        else if (board.IsInStalemate())
            return 0;
        else if (maxExt > 0 && board.IsInCheck()) {
            maxExt--;
            depth++;
        }

        bool quiescence = depth <= 0;
        if (quiescence) {
            var score = Evaluate();
            if (score >= beta) return score;
            if (score > alpha) alpha = score;
        }

        ref Position tr = ref Transpositions[ board.ZobristKey % TTSize ];
        if (dFromRoot > 0
            && tr.Key == board.ZobristKey
            && tr.Depth >= depth
            && (tr.BoundType == 0
                || tr.BoundType < 0 && tr.Score >= beta
                || tr.BoundType > 0 && tr.Score <= alpha)) {
            return tr.Score;
        }

        var moves = board.GetLegalMoves(quiescence);
        var scores = new int[moves.Length];
        for(int i=0; i<moves.Length; i++) {
            if (moves[i] == tr.Move)
                scores[i] = 1000000;
            else if (moves[i].IsCapture)
                scores[i] = 100 * (int)moves[i].CapturePieceType - (int)moves[i].MovePieceType;
        }

        Move bestMove = Move.NullMove;
        for(int i=0; i<moves.Length; i++) {
            if (!MayThink()) throw new OutOfTime();
            var (_, iMax) = scores.Select((score, i) => (score, i)).Skip(i).Max();
            (scores[i], scores[iMax]) = (scores[iMax], scores[i]);
            (moves[i], moves[iMax]) = (moves[iMax], moves[i]);
            var move = moves[i];
            int score;

            board.MakeMove(move);
            score = board.IsRepeatedPosition() ? 0 : - Search(depth - 1, dFromRoot + 1, - beta, - alpha, maxExt);
            board.UndoMove(move);
            if (score > alpha) {
                alpha = score;
                bestMove = move;
                if (alpha >= beta) break;
            }
        }
        tr.Key = board.ZobristKey;
        tr.Move = bestMove;
        tr.BoundType =
            alpha >= beta ? -1 :
            bestMove.IsNull ? 1 : 0;
        tr.Score = alpha;
        tr.Depth = depth;
        if (dFromRoot == 0) bestRootMove = bestMove;
        return alpha;
    }

    int Evaluate()
    {
        int score = 0;
        for(PieceType pt=PieceType.Pawn; pt<PieceType.King; pt++)
            score += PieceValue[(int)pt] * (
                board.GetPieceList(pt, board.IsWhiteToMove).Count -
                board.GetPieceList(pt, !board.IsWhiteToMove).Count);
        score += board.GetLegalMoves().Length + board.GetLegalMoves(true).Length;
        if (board.TrySkipTurn()) {
            // their mobility/threats will only be deducted when we're not in
            // check, but when we are our mobility is mostly decreased anyway
            score -= board.GetLegalMoves().Length + board.GetLegalMoves(true).Length;
            board.UndoSkipTurn();
        }
        return score;
    }

    bool MayThink()
    {
        return bestRootMove.IsNull
            || timer.MillisecondsElapsedThisTurn < timeBudget;
    }
}
