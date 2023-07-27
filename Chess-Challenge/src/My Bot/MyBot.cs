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
		List<Move> allMoves = board.GetLegalMoves().ToList();
		allMoves = allMoves.OrderBy(x => rng.Next()).ToList();//shuffle it to get a random best move at the end

		float bestScore = float.MinValue;
		Move bestMove = allMoves[0];

		foreach (Move move in allMoves)
		{
			board.MakeMove(move);
			float score = -MinMaxPositionScore(board, 0); //don't know yet why this has to be negated
			board.UndoMove(move);
			if (score > bestScore)
			{
				bestScore = score;
				bestMove = move;
			}
		}

		return bestMove;
	}

	float MinMaxPositionScore(Board board, int depth)
	{
		if (depth == 0 || board.IsInsufficientMaterial() || board.IsInCheckmate() || board.GetLegalMoves().Length == 0)
			return Evaulate(board);
		
		List<Move> allMoves = board.GetLegalMoves().ToList();
		
		float bestScore = float.MinValue;
		foreach (Move move in allMoves)
		{
			board.MakeMove(move);
			float score = MinMaxPositionScore(board, depth-1);
			board.UndoMove(move);
			bestScore = Math.Max(bestScore, score);
		}

		return bestScore;
		//Evaulate() already checks who made the move and returns it's value accordingly so I don't actually have to do minmaxing
	}

	float Evaulate(Board board)
	{
		if (board.IsInCheckmate()) return float.MaxValue;
		if (board.IsInsufficientMaterial()) return 0;
		if (board.GetLegalMoves().Length == 0) return 0;

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

		whitePieceValue += KingValue(board, pieceList[5][0]);

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

		blackPieceValue += KingValue(board, pieceList[11][0]);

		return board.IsWhiteToMove ? blackPieceValue - whitePieceValue : whitePieceValue - blackPieceValue;
		//IsWhiteToMove is negatided so positive values are good for the player that just made a move
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