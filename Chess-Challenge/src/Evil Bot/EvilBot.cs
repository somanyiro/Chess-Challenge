using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ChessChallenge.Example;

public class EvilBot : IChessBot
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
		float fileMultiplier = Map(Math.Abs(piece.Square.File-4), 0, 4, 0.5f, 0);
		float rankMultiplier = Map(Math.Abs(piece.Square.Rank-4), 0, 4, 0.5f, 0);

		float earlyPositionMultiplier = 1 + fileMultiplier + rankMultiplier;

		float latePositionMultiplier = piece.IsWhite ? Map(piece.Square.Rank, 0, 9, 1, 3) : Map(piece.Square.Rank, 9, 0, 1, 3);

		return 100 * Lerp(earlyPositionMultiplier, latePositionMultiplier, GamePhase(board));
	}

	float KnightValue(Piece piece)
	{
		float positionMultiplier = piece.Square.File == 0 || piece.Square.File == 7 || piece.Square.Rank == 0 || piece.Square.Rank == 7 ? 0.5f : 1;

		return 300 * positionMultiplier;
	}

	float BishopValue(Piece piece)
	{
		float positionMultiplier = piece.Square.Rank == 0 || piece.Square.Rank == 7 ? 0.5f : 1;

		return 350 * positionMultiplier;
	}

	float KingValue(Board board, Piece piece)
	{
		return 100000;
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