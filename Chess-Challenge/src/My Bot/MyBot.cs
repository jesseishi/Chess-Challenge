using ChessChallenge.API;
using ChessChallenge.Application;
using System;

public class MyBot : IChessBot
{
    // Piece values: Pawn, Knight, Bishop, Rook, Queen, King
    private static readonly int[] pieceValues = { 0, 1, 3, 3, 5, 9, 0 };

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int bestEval = int.MinValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Minimax(board, 3, false);
            board.UndoMove(move);

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int Minimax(Board board, int depth, bool capturesOnly)
    {
        // Are we done?
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            // If we searched for all possible moves, let's now do a quienscence search until we've finished all captions.
            // So infinite depth and with captuersOnly = true.
            if (!capturesOnly)
                // We did not play any moves, so we don't negate the evaluation.
                return Minimax(board, 2, true); // Start quiescence search

            // Else, we've finished the quienscence search and can return the evaluation.
            return Evaluate(board);
        }

        int bestEval = int.MinValue;
        Move[] moves = board.GetLegalMoves(capturesOnly);
        if (moves.Length == 0)
        {
            return Evaluate(board);
        }

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Minimax(board, depth - 1, capturesOnly);
            board.UndoMove(move);

            if (eval > bestEval)
                bestEval = eval;
        }

        return bestEval == int.MinValue ? Evaluate(board) : bestEval;
    }

    private int Evaluate(Board board)
    {
        int eval = EvaluateForWhite(board);
        return eval * (board.IsWhiteToMove ? 1 : -1);
    }

    // Always evaluates from White's perspective
    private int EvaluateForWhite(Board board)
    {
        if (board.IsInCheckmate())
            return board.IsWhiteToMove ? int.MinValue : -int.MaxValue;
        if (board.IsDraw())
            return 0;

        int eval = 0;
        eval += EvaluateMaterial(board);
        //eval += EvaluateKnightCentralization(board);
        //eval += EvaluatePawnAdvancement(board);

        return eval;
    }

    private int EvaluateMaterial(Board board)
    {
        int eval = 0;
        for (int i = 1; i <= 5; i++)
            eval += pieceValues[i] * board.GetPieceList((PieceType)i, true).Count;
        for (int i = 1; i <= 5; i++)
            eval -= pieceValues[i] * board.GetPieceList((PieceType)i, false).Count;
        return eval;
    }

    private int EvaluateKnightCentralization(Board board)
    {
        int eval = 0;
        foreach (var knight in board.GetPieceList(PieceType.Knight, true))
        {
            if (knight.Square.File >= 2 && knight.Square.File <= 5 &&
                knight.Square.Rank >= 2 && knight.Square.Rank <= 5)
                eval += 1;
        }
        foreach (var knight in board.GetPieceList(PieceType.Knight, false))
        {
            if (knight.Square.File >= 2 && knight.Square.File <= 5 &&
                knight.Square.Rank >= 2 && knight.Square.Rank <= 5)
                eval -= 1;
        }
        return eval;
    }

    private int EvaluatePawnAdvancement(Board board)
    {
        int eval = 0;
        foreach (var pawn in board.GetPieceList(PieceType.Pawn, true))
            eval += pawn.Square.Rank;
        foreach (var pawn in board.GetPieceList(PieceType.Pawn, false))
            eval -= (7 - pawn.Square.Rank);
        return eval;
    }
}
