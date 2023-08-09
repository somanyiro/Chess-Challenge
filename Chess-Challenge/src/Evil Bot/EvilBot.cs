﻿using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ChessChallenge.Example;

public class EvilBot : IChessBot
{
	Random rng = new Random();
	float evaulation = 0f;

	public float GetEvaluation()
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
	
		foreach (PieceList list in board.GetAllPieceLists())
		{
			foreach (Piece piece in list)
			{
				float pieceValue;
				switch (piece.PieceType) 
				{
					case PieceType.Pawn:
						pieceValue = PawnValue(board, piece);
						if (piece.IsWhite) whitePieceValue += pieceValue;
						else blackPieceValue += pieceValue;
						break;

					case PieceType.Knight:
						pieceValue = KnightValue(board, piece);
						if (piece.IsWhite) whitePieceValue += pieceValue;
						else blackPieceValue += pieceValue;
						break;

					case PieceType.Bishop:
						pieceValue = 333 * SliderValueMultiplier(board, piece);
						if (piece.IsWhite) whitePieceValue += pieceValue;
						else blackPieceValue += pieceValue;
						break;

					case PieceType.Rook:
						pieceValue = 563 * SliderValueMultiplier(board, piece);
						if (piece.IsWhite) whitePieceValue += pieceValue;
						else blackPieceValue += pieceValue;
						break;

					case PieceType.Queen:
						pieceValue = 950 * SliderValueMultiplier(board, piece);
						if (piece.IsWhite) whitePieceValue += pieceValue;
						else blackPieceValue += pieceValue;
						break;

					case PieceType.King:
						pieceValue = KingValue(board, piece);
						if (piece.IsWhite) whitePieceValue += pieceValue;
						else blackPieceValue += pieceValue;
						break;
					
					default:
						break;
				}
			}
		}

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

	float KnightValue(Board board, Piece piece)
	{
		float positionMultiplier = piece.Square.File == 0 || piece.Square.File == 7 || piece.Square.Rank == 0 || piece.Square.Rank == 7 ? 0.7f : 1;
		float gameStateMultiplier = Map(PositionOpen(board), 0, 1, 1.5f, 1);

		return 305 * positionMultiplier * gameStateMultiplier;
	}

	float SliderValueMultiplier(Board board, Piece piece)
	{
		return Map(PositionOpen(board), 0, 1, 0.8f, 1.1f);
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