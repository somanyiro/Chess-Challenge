using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class MyBot : IChessBot
{
	float evaluation = 0f;
	Random rnd = new Random();

	public float GetEvaluation()
	{
		return evaluation;
	}

	public Move Think(Board board, Timer timer)
	{
		(Move, float) bestMove = GetBestMove(board, float.MinValue, float.MaxValue, 3);
		evaluation = bestMove.Item2;
		return bestMove.Item1;
	}

	///<summary>
	///Uses a Min Max algorithm with alpha beta pruning to calculate the best move at a given depth
	///</summary>
	(Move, float) GetBestMove(Board board, float alpha, float beta, int depth)
	{
		List<Move> allMoves = board.GetLegalMoves().ToList();
		allMoves = allMoves.OrderBy(x => rnd.Next()).ToList();
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

		float result = 0;

		foreach (Piece piece in board.GetAllPieceLists().SelectMany(x => x))
		{
			float pieceValue = 0;
			switch (piece.PieceType) 
			{
				case PieceType.Pawn:
					pieceValue = 10.0f * PawnValue(board, piece);
					break;

				case PieceType.Knight:
					pieceValue = 30.5f * KnightValue(board, piece);
					break;

				case PieceType.Bishop:
					pieceValue = 33.3f * SliderValueMultiplier(board, piece);
					break;

				case PieceType.Rook:
					pieceValue = 56.3f * SliderValueMultiplier(board, piece);
					break;

				case PieceType.Queen:
					pieceValue = 95.0f * SliderValueMultiplier(board, piece);
					break;

				case PieceType.King:
					pieceValue = KingValue(board, piece);
					break;
				
				default:
					break;
			}
			if (piece.IsWhite) result += pieceValue;
			else result -= pieceValue;
		}

		return result;
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
			{0.90f, 0.95f, 1.05f, 1.10f},
			{0.90f, 0.95f, 1.05f, 1.15f},
			{0.90f, 0.95f, 1.10f, 1.20f},
			{0.97f, 1.03f, 1.17f, 1.27f},
			{1.06f, 1.12f, 1.25f, 1.40f},
			{5.63f, 5.63f, 5.63f, 5.63f},
		};

		float[,] lateGameValueTable = 
		{
			{1.20f, 1.05f, 0.95f, 0.90f},
			{1.20f, 1.05f, 0.95f, 0.90f},
			{1.25f, 1.10f, 1.00f, 0.95f},
			{1.33f, 1.17f, 1.07f, 1.00f},
			{1.45f, 1.29f, 1.16f, 1.05f},
			{5.63f, 5.63f, 5.63f, 5.63f},
		};

		int relativeRank = piece.IsWhite ? piece.Square.Rank-1 : (int)Map(piece.Square.Rank, 0, 7, 7, 0)-1;

		float positionMultiplier = Lerp(
			earlyGameValueTable[relativeRank, Fold8(piece.Square.File)],
			lateGameValueTable[relativeRank, Fold8(piece.Square.File)],
			GamePhase(board)
		);

		return positionMultiplier;
	}

	float KnightValue(Board board, Piece piece)
	{
		float positionMultiplier = Fold8(piece.Square.File) == 0 || Fold8(piece.Square.Rank) == 0 ? 0.7f : 1;
		float gameStateMultiplier = Map(PositionOpen(board), 0, 1, 1.5f, 1);

		return positionMultiplier * gameStateMultiplier;
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
		return Map(
			board.GetAllPieceLists().SelectMany(x => x).Count(), 
			2, 32, 1, 0);
	}

	float Map(float value, float old_min, float old_max, float new_min, float new_max)
	{
		return new_min + (value - old_min) * (new_max - new_min) / (old_max - old_min);
	}

	float Lerp(float a, float b, float f)
	{
		return a + f * (b - a);
	}

	/// <summary>Converts an int from 0-7 to 0-3-0</summary>
	int Fold8(int x)
	{
		return (int)-Math.Abs(0.86f * x - 3) + 3; 
	}
}