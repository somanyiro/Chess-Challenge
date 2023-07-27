using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class MyBot : IChessBot
{
    Random rng = new Random();

    public Move Think(Board board, Timer timer)
    {
        (Move, float) bestMove = FindBestMove(board, board.IsWhiteToMove, 2, 3);
        Console.WriteLine(bestMove.Item2);

        return bestMove.Item1;
    }
    
    (Move, float score) FindBestMove(Board board, bool forWhite, int depth, int maxDepth)
    {
        List<Move> moves = board.GetLegalMoves().ToList();

        moves = moves.OrderBy(x => rng.Next()).ToList();//shuffle it to get a random best move at the end
        //moves = moves.OrderBy(x => MovePriority(board, x)).ToList();//order to have checks and captures first

        Move bestMove = moves[0];
        float bestScore = -100000;

        foreach (var move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                bestMove = move;
                bestScore = 100000;
                break;
            }

            if (MoveIsStalemate(board, move))
            {
                continue;
            }

            //int addedDepth = MovePriority(board, move);
            //depth += addedDepth;

            board.MakeMove(move);

            if (depth > 0 && maxDepth > 0)
            {
                (Move, float) result = FindBestMove(board, !forWhite, depth-1, maxDepth-1);
                if (-result.Item2 > bestScore)
                {
                    bestMove = move;
                    bestScore = -result.Item2;
                }
            }
            else
            {
                float score = Evaulate(board, forWhite);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            board.UndoMove(move);
            //depth -= addedDepth;
        }

        return (bestMove, bestScore);
    }
    
    float Evaulate(Board board, bool isWhite)
    {
        float whitePieceValue = 0;
        float blackPieceValue = 0;

        PieceList[] pieceList = board.GetAllPieceLists();
    
        foreach (var pawn in pieceList[0])
        {
            whitePieceValue += PawnValue(board, pawn);
        }

        foreach (var knight in pieceList[1])
        {
            whitePieceValue += KnightValue(knight);
        }

        if (pieceList[2].Count >= 2) whitePieceValue += 150; // bishop pair STRONG

        foreach (var bishop in pieceList[2])
        {
            whitePieceValue += BishopValue(bishop);
        }

        whitePieceValue += pieceList[3].Count * 500;
        whitePieceValue += pieceList[4].Count * 900;

        whitePieceValue += KingValue(board , pieceList[5][0]);

        foreach (var pawn in pieceList[6])
        {
            blackPieceValue += PawnValue(board, pawn);
        }

        foreach (var knight in pieceList[7])
        {
            blackPieceValue += KnightValue(knight);
        }

        if (pieceList[8].Count >= 2) blackPieceValue += 150;

        foreach (var bishop in pieceList[8])
        {
            blackPieceValue += BishopValue(bishop);
        }

        blackPieceValue += pieceList[9].Count * 500;
        blackPieceValue += pieceList[10].Count * 900;

        blackPieceValue += KingValue(board , pieceList[11][0]);

        return isWhite ? whitePieceValue - blackPieceValue : blackPieceValue - whitePieceValue;
    }

    int MovePriority(Board board, Move move)
    {
        int score = 0;

        if (MoveIsCheck(board, move)) score += 5;

        Piece capture = board.GetPiece(move.TargetSquare);
        
        if (capture.IsQueen) score += 4;
        if (capture.IsRook) score += 3;
        if (capture.IsBishop) score += 2;
        if (capture.IsKnight) score += 2;
        if (capture.IsPawn) score += 1;

        score = (int)Math.Round(Map(score, 0, 8, 0, 2));//converting it so I can just add it to the search depth

        return score;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsStalemate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isStalemate = board.IsDraw();
        board.UndoMove(move);
        return isStalemate;
    }

    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    float PawnValue(Board board, Piece piece)
    {
        float fileMultiplier = Map(Math.Abs(piece.Square.File-4), 0, 4, 0.5f, 0);
        float rankMultiplier = Map(Math.Abs(piece.Square.Rank-4), 0, 4, 0.5f, 0);

        float earlyPositionMultiplier = 1 + fileMultiplier + rankMultiplier;

        float latePositionMultiplier = piece.IsWhite ? Map(piece.Square.Rank, 0, 9, 1, 3) : Map(piece.Square.Rank, 9, 0, 1, 3);

        return 100 * Lerp(earlyPositionMultiplier, latePositionMultiplier, GamePhase(board));
    }

    float KnightValue(Piece piece)
    {
        float positionMultiplier = piece.Square.File == 0 || piece.Square.File == 7 || piece.Square.Rank == 0 ? 0.5f : 1;

        return 300 * positionMultiplier;
    }

    float BishopValue(Piece piece)
    {
        float positionMultiplier = piece.Square.Rank == 0 ? 0.5f : 1;

        return 300 * positionMultiplier;
    }

    float KingValue(Board board, Piece piece)
    {
        //this doesn't want to work very well
        /*
        float earlyPositionFileMultiplier = Map(Math.Abs(piece.Square.File-4), 0, 4, 1, 1.1f);
        float earlyPositionRankMultiplier = Map(piece.Square.Rank, 0, 7, 2f, 1);
        float earlyPositionMultiplier = earlyPositionFileMultiplier * earlyPositionRankMultiplier;
        float latePositionFileMultiplier = Map(Math.Abs(piece.Square.File-4), 0, 4, 1.1f, 1);
        float latePositionRankMultiplier = Map(Math.Abs(piece.Square.Rank-4), 0, 4, 1.1f, 1);
        float latePositionMultiplier = latePositionFileMultiplier * latePositionRankMultiplier;
        */
        return 100000; /** Lerp(earlyPositionMultiplier, latePositionMultiplier, GamePhase(board));*/
    }

    float GamePhase(Board board)
    {
        int numberOfPieces = 0;
        foreach (var pieceList in board.GetAllPieceLists())
        {
            numberOfPieces += pieceList.Count;
        }
        return Map(numberOfPieces, 2, 32, 1, 0);
    }
    

	float Map(float value, float old_min, float old_max, float new_min, float new_max)
	{
		return new_min + (value - old_min) * (new_max - new_min) / (old_max - old_min);
	}

    float Lerp(float a, float b, float f)
	{
		return a + f * (b - a);
	}
}