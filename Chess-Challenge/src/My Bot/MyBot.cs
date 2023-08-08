using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class MyBot : IChessBot
{
	Random rng = new Random();
	float evaulation = 0f;

	public float GetEvaulation()
	{
		return evaulation;
	}

	public Move Think(Board board, Timer timer)
	{
		(Move, float) bestMove = GetBestMove(board, float.MinValue, float.MaxValue, 2);
		evaulation = bestMove.Item2;
		return bestMove.Item1;
	}

	///<summary>
	///Uses a Min Max algorithm with alpha beta pruning to calculate the best move at a given depth
	///</summary>
	(Move, float) GetBestMove(Board board, float alpha, float beta, int depth)
	{		
		List<Move> allMoves = board.GetLegalMoves().ToList();
		allMoves = allMoves.OrderBy(x => rng.Next()).ToList();
		allMoves = allMoves.OrderByDescending(x => MovePriority(board, x)).ToList();
		
		float bestScore = board.IsWhiteToMove ? float.MinValue : float.MaxValue;
		Move bestMove = allMoves[0];
		foreach (Move move in allMoves)
		{
			board.MakeMove(move);
			float score;
			if (depth == 0 || board.IsInsufficientMaterial() || board.IsInCheckmate() || board.GetLegalMoves().Length == 0)
				score = Evaulate(board);
			else
			{
				score = GetBestMove(board, alpha, beta, depth-1).Item2;
			}
			board.UndoMove(move);
			if ((board.IsWhiteToMove && score > bestScore) || (!board.IsWhiteToMove && score < bestScore))
			{
				bestScore = score;
				bestMove = move;
			}
			if (board.IsWhiteToMove && score > alpha) alpha = score;
			if (!board.IsWhiteToMove && score < beta) beta = score;
			if (beta < alpha) break;
		}

		return (bestMove, bestScore);
	}

	float Evaulate(Board board)
	{
		if (board.IsInCheckmate()) return board.IsWhiteToMove ? float.MinValue : float.MaxValue;
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

		whitePieceValue += pieceList[3].Count * 563;
		whitePieceValue += pieceList[4].Count * 950;

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

		blackPieceValue += pieceList[9].Count * 563;
		blackPieceValue += pieceList[10].Count * 950;

		blackPieceValue += KingValue(board, pieceList[11][0]);

		return whitePieceValue - blackPieceValue;
	}

	int MovePriority(Board board, Move move)
	{
		int score = 0;

		board.MakeMove(move);
		if (board.IsInCheck()) score += 5;
		board.UndoMove(move);

		return score + (int)board.GetPiece(move.TargetSquare).PieceType;
	}

	float PawnValue(Board board, Piece piece)
	{
		//this is accoring to Hans Berliner's system but I don't know if the 7th and 8th rank are correct
		float[,] earlyGameValueTable = 
		{
			{0.90f, 0.95f, 1.05f, 1.10f, 1.10f, 1.05f, 0.95f, 0.90f},
			{0.90f, 0.95f, 1.05f, 1.15f, 1.15f, 1.05f, 0.95f, 0.90f},
			{0.90f, 0.95f, 1.10f, 1.20f, 1.20f, 1.10f, 0.95f, 0.90f},
			{0.97f, 1.03f, 1.17f, 1.27f, 1.27f, 1.17f, 1.03f, 0.97f},
			{1.06f, 1.12f, 1.25f, 1.40f, 1.40f, 1.25f, 1.12f, 1.06f},
			{5.63f, 5.63f, 5.63f, 5.63f, 5.63f, 5.63f, 5.63f, 5.63f},
		};

		float[,] lateGameValueTable = 
		{
			{1.20f, 1.05f, 0.95f, 0.90f, 0.90f, 0.95f, 1.05f, 1.20f},
			{1.20f, 1.05f, 0.95f, 0.90f, 0.90f, 0.95f, 1.05f, 1.20f},
			{1.25f, 1.10f, 1.00f, 0.95f, 0.95f, 1.00f, 1.10f, 1.25f},
			{1.33f, 1.17f, 1.07f, 1.00f, 1.00f, 1.07f, 1.17f, 1.33f},
			{1.45f, 1.29f, 1.16f, 1.05f, 1.05f, 1.16f, 1.29f, 1.45f},
			{5.63f, 5.63f, 5.63f, 5.63f, 5.63f, 5.63f, 5.63f, 5.63f},
		};

		int relativeRank = piece.IsWhite ? piece.Square.Rank-1 : (int)Map(piece.Square.Rank, 0, 7, 7, 0)-1;

		float positionMultiplier = Lerp(
			earlyGameValueTable[relativeRank, piece.Square.File],
			lateGameValueTable[relativeRank, piece.Square.File],
			GamePhase(board)
		);

		return 100 * positionMultiplier;
	}

	float KnightValue(Piece piece)
	{
		float positionMultiplier = piece.Square.File == 0 || piece.Square.File == 7 || piece.Square.Rank == 0 || piece.Square.Rank == 7 ? 0.5f : 1;

		return 305 * positionMultiplier;
	}

	float BishopValue(Piece piece)
	{
		float positionMultiplier = piece.Square.Rank == 0 || piece.Square.Rank == 7 ? 0.5f : 1;

		return 333 * positionMultiplier;
	}

	float KingValue(Board board, Piece piece)
	{
		return 100000;
	}

	/// <summary>Returns 0-1 depending on how open the position is which is determinaed by pawn structure</summary>
	float PositionOpen(Board board)
	{
		return Map(
			board.GetPieceList(PieceType.Pawn, true).Count() + board.GetPieceList(PieceType.Pawn, false).Count(),
			0, 16, 0, 1);
	}

	/// <summary>Returns 0-1 depending on the number of pieces left on the board</summary>
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