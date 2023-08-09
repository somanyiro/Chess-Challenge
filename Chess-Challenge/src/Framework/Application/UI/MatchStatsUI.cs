using Raylib_cs;
using System.Numerics;
using System;

namespace ChessChallenge.Application
{
    public static class MatchStatsUI
    {
        public static void DrawMatchStats(ChallengeController controller)
        {
            if (controller.PlayerWhite.IsBot && controller.PlayerBlack.IsBot)
            {
                int nameFontSize = UIHelper.ScaleInt(40);
                int regularFontSize = UIHelper.ScaleInt(35);
                int headerFontSize = UIHelper.ScaleInt(45);
                Color col = new(180, 180, 180, 255);
                Vector2 startPos = UIHelper.Scale(new Vector2(1500, 250));
                float spacingY = UIHelper.Scale(35);

                DrawNextText($"Game {controller.CurrGameNumber} of {controller.TotalGameCount}", headerFontSize, Color.WHITE);
                startPos.Y += spacingY * 2;

                DrawStats(controller.BotStatsA);
                startPos.Y += spacingY * 2;
                DrawStats(controller.BotStatsB);
           

                void DrawStats(ChallengeController.BotMatchStats stats)
                {
                    DrawNextText(stats.BotName + ":", nameFontSize, Color.WHITE);
                    DrawNextText($"Score: +{stats.NumWins} ={stats.NumDraws} -{stats.NumLosses}", regularFontSize, col);
                    DrawNextText($"Num Timeouts: {stats.NumTimeouts}", regularFontSize, col);
                    DrawNextText($"Num Illegal Moves: {stats.NumIllegalMoves}", regularFontSize, col);
                }
           
                void DrawNextText(string text, int fontSize, Color col)
                {
                    UIHelper.DrawText(text, startPos, fontSize, 1, col);
                    startPos.Y += spacingY;
                }
            }

            float myBotEvaulation = 
                controller.PlayerWhite.PlayerType == ChallengeController.PlayerType.MyBot ? 
                controller.PlayerWhite.Bot.GetEvaluation() : 
                controller.PlayerBlack.Bot.GetEvaluation();

            UIHelper.DrawText(
                $"MyBot evaulation: {myBotEvaulation}",
                UIHelper.Scale(new Vector2(1500, 800)),
                UIHelper.ScaleInt(35),
                1,
                Color.WHITE);
            
            float evilBotEvaulation;

            if (controller.PlayerWhite.PlayerType == ChallengeController.PlayerType.EvilBot)
                evilBotEvaulation = controller.PlayerWhite.Bot.GetEvaluation();
            else if (controller.PlayerBlack.PlayerType == ChallengeController.PlayerType.EvilBot)
                evilBotEvaulation = controller.PlayerBlack.Bot.GetEvaluation();
            else return;

            UIHelper.DrawText(
                $"EvilBot evaulation: {evilBotEvaulation}",
                UIHelper.Scale(new Vector2(1500, 850)),
                UIHelper.ScaleInt(35),
                1,
                Color.WHITE);

        }
    }
}