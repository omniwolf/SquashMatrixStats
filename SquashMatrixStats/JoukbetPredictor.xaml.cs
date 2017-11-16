using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace SquashMatrixStats {
    /// <summary>
    /// Interaction logic for JoukbetPredictor.xaml
    /// </summary>
    public partial class JoukbetPredictor : Window {
        MainWindow mw;

        // format is:
        // less than 30 days, 30-60 days, 60 days - 6 month, 6+ months.
        // so for 30 days, the scale is 1 (100%), if the game is 30-60 days old, the value gets scaled to 0.8 (80%), etc
        decimal[] dateScale = new decimal[4] { 1M, 0.8M, 0.5M, 0.1M };

        // this one is where we tally up how many games we've had in the various time brackets.
        // the idea here is that for example if we've _only_ had games in 6+ months bracket, then the dateModifier in the calculateScore() method
        // is kinda useless because each game is modified by the same amount, so when it's combined with the matrix score, even though all the
        // games were played 6+ months ago, they still have 25% relevance.  By keeping track of our spread of game ages, we can modify the 25%
        // value that game history contributes to be more realistic (eg if all games are 6+ months ago, make it 10% of 25%, rather than the full 25%).
        int[] ageCount = new int[4] { 0, 0, 0, 0 };

        public JoukbetPredictor(MainWindow _mw) {
            InitializeComponent();

            mw = _mw;
        }

        private void GoButton_Click(object sender, RoutedEventArgs e) {

            // this is a max of how much the historic games contribute to the final score.
            decimal historyRatio = 0.25M;

            List<Result> player1Results = MatrixInterface.getResults(Player1Input.Text);
            if (player1Results == null || player1Results.Count == 0) {
                Debug.Print("couldn't find player.. or something");
                return;
            }
            List<Result> clashes = new List<Result>();
            int player1Wins = 0;
            int player2Wins = 0;

            // tracking if it's a 3-0 or 3-1 or 3-2 win.  We don't do best of 3 or best of 1 games (yet?)
            int player130 = 0;
            int player131 = 0;
            int player132 = 0;

            int player230 = 0;
            int player231 = 0;
            int player232 = 0;

            // i think we can get rid of these two vars, i don't use them anymore
            int player1TotalGames = 0;
            int player2TotalGames = 0;

            // scores go directly towards figuring out the likeliness to win value
            decimal history1Score = 0;
            decimal history2Score = 0;
            decimal rating1Score = 0;
            decimal rating2Score = 0;

            /**********************
             * Play history scoring
             * ********************/
            foreach (Result res in player1Results) {  // go through all of player 1's history
                if (res.opponentID == Player2Input.Text) {  // if we found a match where they played player 2
                    clashes.Add(res);  // I don't actually use this list anywhere (yet)
                    Debug.Print("Found a clash on " + res.date.ToString("dd/MM/yyyy"));
                    if(res.playerGames >= res.opponentGames) {  // player wins
                        player1Wins++;
                        Debug.Print("-- Player 1 beat player 2: " + res.playerGames + " to " + res.opponentGames + ".  They have won " + player1Wins + " time(s)");
                        MatchesWon1Label.Content = player1Wins.ToString();
                        if(res.playerGames == 3) {  // only record best of 5 matches
                            switch(res.opponentGames) {
                                case 0:
                                    player130++;
                                    MatchesWon301Label.Content = player130.ToString();
                                    break;
                                case 1:
                                    player131++;
                                    MatchesWon311Label.Content = player131.ToString();
                                    break;
                                case 2:
                                    player132++;
                                    MatchesWon321Label.Content = player132.ToString();
                                    break;
                            }
                        }
                    }
                    else {  // opponent wins
                        player2Wins++;
                        Debug.Print("-- player 2 beat player 1: " + res.opponentGames + " to " + res.playerGames + ".  They have won " + player2Wins + " time(s)");

                        MatchesWon2Label.Content = player2Wins.ToString();
                        if(res.opponentGames == 3) {
                            switch(res.playerGames) {
                                case 0:
                                    player230++;
                                    MatchesWon302Label.Content = player230.ToString();
                                    break;
                                case 1:
                                    player231++;
                                    MatchesWon312Label.Content = player231.ToString();
                                    break;
                                case 2:
                                    player232++;
                                    MatchesWon322Label.Content = player232.ToString();
                                    break;
                            }
                        }
                    }
                    
                    player1TotalGames += res.playerGames;
                    player2TotalGames += res.opponentGames;
                    Tuple<decimal, decimal> joukbetScores = calculateScore(res);

                    Debug.Print("-- Player 1 history score is " + joukbetScores.Item1 + " and player 2 is " + joukbetScores.Item2);
                    history1Score += joukbetScores.Item1;
                    history2Score += joukbetScores.Item2;
                    Debug.Print("---- Player 1 cumulative history score is " + history1Score + " and player 2 is " + history2Score);
                }
            }

            // next let's use their current ratings to give modify the score
            double player2Rating = MatrixInterface.getCurrentRating(Player2Input.Text);
            if (player2Rating == 0) {
                MessageBox.Show("Cannot find player 2's matrix?  are you sure this player exists?");
                return;
            }
            decimal ratingDifferential = (decimal)player1Results.First().rating - (decimal)player2Rating;
            Debug.Print("ratingDifferential = " + ratingDifferential + " and player 1's matrix is " + (decimal)player1Results.First().rating);

            //if(player1TotalGames == 0 && player2TotalGames == 0) {  // never played, let's just use the matrix.
            if(ratingDifferential > 40) {  // player 1 is more than 40 above, 100% chance to win
                rating1Score = 1;
                Debug.Print("ratingdiff is bigger than 40 (p1 higher), so rating1score is " + rating1Score.ToString());
            }
            else if(ratingDifferential > 30) {  // less than 40, greater than 30
                rating1Score = (((ratingDifferential - 30) / 2) + 95) / 100;
                rating2Score = 1 - rating1Score;
                Debug.Print("ratingDiff is 30-40 (p1 higher), so rating should be between 0.95 and 1: " + rating1Score.ToString());
            }
            else if(ratingDifferential > 0) {
                rating1Score = (((ratingDifferential / 30) * 45) + 50) / 100;
                rating2Score = 1 - rating1Score;
                Debug.Print("ratingDiff is between 0 and 30 (p1 higher), so rating1score should be between 0.5 and 0.95: " + rating1Score);
            }
            else if(ratingDifferential < -40) {  // player 2 is more than 40 above
                rating2Score = 1;
                Debug.Print("ratingdiff is less than -40 (p2 higher), so rating2score is " + rating2Score.ToString());

            }
            else if(ratingDifferential < -30) {  // less than 40, greater than 30
                rating2Score = (((System.Math.Abs(ratingDifferential) - 30) / 2) + 95) / 100;
                rating1Score = 1 - rating2Score;
                Debug.Print("ratingDiff is -30 and -40 (p2 higher), so rating should be between 0.95 and 1: " + rating2Score.ToString());

            }
            else if(ratingDifferential < 0) {  // player 2 rating is between 0 and 30 higher
                rating2Score = (((System.Math.Abs(ratingDifferential) / 30) * 45) + 50) / 100;
                rating1Score = 1 - rating2Score;
                Debug.Print("ratingDiff is between 0 and -30 (p2 higher), so rating2score should be between 0.5 and 0.95: " + rating2Score);

            }

            decimal normalisedHistoryScore = 0;
            if (history1Score != 0 || history2Score != 0) {  // don't divide by 0!
                normalisedHistoryScore = history1Score / (history1Score + history2Score);
            }
            Debug.Print("normalised history score is now " + (normalisedHistoryScore*100).ToString("###.##") + "%");

            // now we scale back the amount the historyScore actually affects the ratingScore, depending on the youngest game
            // by default (assuming we have a recent clash), historyScore contributes 25% (historyRatio variable) to the final score
            if (ageCount[0] != 0) {  // we have a game in the last 30 days.  no need to change anything as the ratio is already set at this
                Debug.Print("most recent game is in the last 30 days, historyRatio is at max: " + (historyRatio * 100).ToString("###.##") + "%");
            }
            if (ageCount[0] == 0 & ageCount[1] != 0) {  // most recent game is 30-60 days ago
                historyRatio = historyRatio * dateScale[1];
                Debug.Print("most recent game is 30-60 days ago, so we scale back the history ratio to " + (historyRatio*100).ToString("###.##") + "%");
            }
            else if (ageCount[0] == 0 && ageCount[1] == 0 && ageCount[2] != 0) {  // most recent game is 2-6 month ago
                historyRatio = historyRatio * dateScale[2];
                Debug.Print("most recent game is 2-6 monhts ago, so we scale back the history ratio to " + (historyRatio * 100).ToString("###.##") + "%");
            }
            else if (ageCount[0] == 0 && ageCount[1] == 0 && ageCount[2] == 0 && ageCount[3] != 0) {  // most recent game is 6+ months ago
                historyRatio = historyRatio * dateScale[3];
                Debug.Print("most recent game was 6+ months ago, so we scale back the history ratio to " + (historyRatio * 100).ToString("###.##") + "%");
            }  
            else { // else all age brackets are 0 - we have no historic clashes
                historyRatio = 0;
                Debug.Print("players have never played, so reduce the history ratio to 0, so the rating contribution is 100%");
            }
            // the rating ratio is just whatever is leftover from the historyRatio.  at this stage the historyRatio will be a max of 25%,
            // so the rating ratio will be at least 75%, depending on the age of the most recent game
            decimal player1Likeliness = ((1 - historyRatio) * rating1Score) + (historyRatio * normalisedHistoryScore);
            decimal player2Likeliness = ((1 - historyRatio) * rating2Score) + (historyRatio * (1 - normalisedHistoryScore));

            LikelinessToWin1Label.Content = (player1Likeliness * 100).ToString("###.##") + "%";
            LikelinessToWin2Label.Content = (player2Likeliness * 100).ToString("###.##") + "%";
            Debug.Print("-------------------");
        }



        /// <summary>
        /// takes in a match, and returns a tuple with the score of both players. 
        /// the first item is the player's score, the second is the opponent's score
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private Tuple<decimal, decimal> calculateScore(Result res) {
            decimal winnerGameModifier = 0;
            decimal loserGameModifier = 0;
            decimal dateModifier = 0M;
            TimeSpan gameAge = DateTime.Today - res.date;

            int winnerGames = 0;
            int winnerPoints = 0;
            int loserGames = 0;
            int loserPoints = 0;

            // so we don't have to duplicate code, i'm abstracting the winner and loser from the player/opponent
            if(res.playerGames > res.opponentGames) {
                winnerGames = res.playerGames;
                winnerPoints = res.playerPoints;
                loserGames = res.opponentGames;
                loserPoints = res.opponentPoints;
            }
            else {
                winnerGames = res.opponentGames;
                winnerPoints = res.opponentPoints;
                loserGames = res.playerGames;
                loserPoints = res.playerPoints;
            }

            if(winnerGames >= 3) {  // ie this is a best of 5 match, we'll use games
                Debug.Print("-- this was best of 5");
                switch(loserGames) {
                    case 0:
                        winnerGameModifier = 1;
                        Debug.Print("-- 30winnerGameModifier is now " + winnerGameModifier + " and loserGameModifier is " + loserGameModifier);
                        break;
                    case 1:
                        winnerGameModifier = 0.8M;
                        loserGameModifier = 0.2M;
                        Debug.Print("-- 31winnerGameModifier is now " + winnerGameModifier + " and loserGameModifier is " + loserGameModifier);
                        break;
                    case 2:
                        winnerGameModifier = 0.5M;
                        loserGameModifier = 0.4M;
                        Debug.Print("-- 32winnerGameModifier is now " + winnerGameModifier + " and loserGameModifier is " + loserGameModifier);
                        break;
                }
            }
            else if(winnerGames >= 2) {  // best of 3, still use games
                Debug.Print("-- this was best of 3");
                switch(loserGames) {
                    case 0:
                        winnerGameModifier = 1;
                        Debug.Print("--20 winnerGameModifier is now " + winnerGameModifier + " and loserGameModifier is " + loserGameModifier);
                        break;
                    case 1:
                        winnerGameModifier = 0.7M;
                        loserGameModifier = 0.3M;
                        Debug.Print("-- 21winnerGameModifier is now " + winnerGameModifier + " and loserGameModifier is " + loserGameModifier);
                        break;
                }
            }
            else if(winnerGames == 1) {  // only 1 game played, probably a timed tourney, using points to figure out the modifier...
                int pointsDiff = winnerPoints - loserPoints;  // i don't really handle the scenario where the winner has less ponits than the loser.  shouldn't be required though...
                Debug.Print("-- this was best of 1, so we use points.  the points difference is " + pointsDiff);
                if(pointsDiff > 30) {
                    winnerGameModifier = 1;
                    Debug.Print("-- winnerGameModifier is now " + winnerGameModifier + " and loserGameModifier is " + loserGameModifier);
                }
                else if(pointsDiff > 0) {  // the points diff will be between 0 and 30, divide by 60 to scale it down to between 0 and 0.5.
                    winnerGameModifier = (pointsDiff / 60M) + 0.5M;  // as 0.5 is an even score, we add the scaled down value to 0.5, so they approach 1 as the points diff approaches 30
                    loserGameModifier = 1 - winnerGameModifier;  // reciprocal - nothing fancy for the loser, they just get what the winner doesn't take
                    Debug.Print("-- winnerGameModifier is now " + winnerGameModifier.ToString() + " and loserGameModifier is " + loserGameModifier.ToString());

                }
                // else modifiers stay at 0.
            }

            if(gameAge.TotalDays < 30) {  // played within a month
                dateModifier = dateScale[0];  // using an array here so we can more easily edit these scale values later if we have to
                ageCount[0]++;
                Debug.Print("-- was a recent match, dateModifier is " + dateModifier + " we've had " + ageCount[0] + " game(s) in this time bracket");
            }
            else if(gameAge.TotalDays < 60) {  // played between 1 and 2 months ago
                dateModifier = dateScale[1];
                ageCount[1]++;
                Debug.Print("-- was a match 30-60 days ago, dateModifier is " + dateModifier + " we've had " + ageCount[1] + " game(s) in this time bracket");
            }
            else if(gameAge.TotalDays < 182) {  // played between 2 and 6 months ago
                dateModifier = dateScale[2];
                ageCount[2]++;
                Debug.Print("-- was a match 2-6 months ago, dateModifier is " + dateModifier + " we've had " + ageCount[2] + " game(s) in this time bracket");
            }
            else {  // 6+ months ago
                dateModifier = dateScale[3];
                ageCount[3]++;
                Debug.Print("-- was a match 6+ months ago, dateModifier is " + dateModifier + " we've had " + ageCount[3] + " game(s) in this time bracket");
            }

            // convert back from winner/loser format to player/opponent format
            if(res.playerGames > res.opponentGames) {  // the first item in the tuple needs to be the player, the second is the opponent
                return new Tuple<decimal, decimal>(winnerGameModifier * dateModifier, loserGameModifier * dateModifier);
            }
            else {
                return new Tuple<decimal, decimal>(loserGameModifier * dateModifier, winnerGameModifier * dateModifier);
            }
        }
    }
}
