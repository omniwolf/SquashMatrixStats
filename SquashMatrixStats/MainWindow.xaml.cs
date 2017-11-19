using System;
using System.Net.Http;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation;


namespace SquashMatrixStats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // i wish this worked
        private void PlayerNumber_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PlayerNumber.SelectAll();
        }

        private void GetStats_Button_Click(object sender, RoutedEventArgs e) {

            if(PlayerNumber.Text == "9589") {
                ImageBrush myBrush = new ImageBrush();
                myBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/kenneth.png"));
                mainWin.Background = myBrush;
            }

            List<Result> allResults = MatrixInterface.getResults(PlayerNumber.Text);
            if (allResults == null || allResults.Count == 0 ) {
                MessageBox.Show("Squash matrix returned no results, wrong player name or maybe you've been blocked!");
                return;
            }

            Debug.Print("there are " + allResults.Count + " entries");
            List<Result> topResults = allResults.OrderByDescending(o => o.rating).ToList();

            Debug.Print("highest rating is: " + topResults.Last().rating.ToString() + " and this was done on: " + topResults.First().date.ToString());
            HighestMatrixLabel.Content = topResults.First().rating.ToString();
            HighestMatrixLabel.ToolTip = topResults.First().date.ToString("dd/MM/yyyy");
            int lowCount = topResults.Count - 1;
            while(topResults[lowCount].rating == 0) {
                Debug.Print("lowCount is : " + lowCount.ToString() + " and rating is " + topResults[lowCount].rating.ToString());
                lowCount--;
            }
            LowestMatrixLabel.Content = topResults[lowCount].rating.ToString();
            LowestMatrixLabel.ToolTip = topResults[lowCount].date.ToString("dd/MM/yyyy");

            // all time most played
            IOrderedEnumerable<IGrouping<string, Result>> groupedResults = allResults.GroupBy(o => o.opponentName).OrderByDescending(group => group.Count());
            int groupedCount = 0;
            StringBuilder topTenMostPlayed = new StringBuilder();

            foreach(var grp1 in groupedResults) {
                if(groupedCount == 0) {  // on the first iteration, just put the results in the actual label contents
                    AllTimeMostPlayedLabel.Content = grp1.First().opponentName + " (" + grp1.Count().ToString() + ")";
                }
                else if(groupedCount < 20) {  // for the next.. 19? interations, put those next 19 results in a string that we'll eventually put in the tooltip
                    topTenMostPlayed.AppendLine(grp1.First().opponentName + " (" + grp1.Count().ToString() + ")");
                }
                else {
                    break;  // only want to look at the first 20
                }
                groupedCount++;
            }
            AllTimeMostPlayedLabel.ToolTip = topTenMostPlayed.ToString();

            UniquePlayersPlayedLabel.Content = groupedResults.Count();

            // look how many variables i need for these calculations.  seems like a lot, probably a better way?
            int winStreak = 0;  // tracking current streak
            int loseStreak = 0;
            int highWinStreak = 0;  // keep a memory of our longest streak
            int highLoseStreak = 0;
            DateTime winStreakStart = new DateTime(1900, 1, 1);  // longest win streak start date
            DateTime winStreakStart_temp = allResults.First().date;  // because the current streak might be our longest, keep a memory of when it started in case it is
            DateTime winStreakEnd = new DateTime(1900, 1, 1);  // no need to keep in memory the end of the streak, as we'll know by the time we hit it if the streak was our best yet
            DateTime loseStreakStart = new DateTime(1900, 1, 1);
            DateTime loseStreakStart_temp = allResults.First().date;
            DateTime loseStreakEnd = new DateTime(1900, 1, 1);
            bool currentlyOnWinStreak = false;  // code needs to know if we're currently on a win streak or currently on a lose streak

            int bestWin = 0;  // at the same time, we record the index of the best win so we can refer to it later
            int worstLoss = 0;
            int bestWinRecent = 0;  // same as above, but we only record for the last year
            int worstLossRecent = 0;

            // these vars are for how much your matrix has gone up or down over a period of time
            bool isThreeMonthsDelta = false;
            double threeMonthMatrix = 0;
            bool isTwelveMonthsDelta = false;
            double twelveMothMatrix = 0;

            int loopCount = 0;

            // we loop through all the results, in order from newest to oldest.  
            foreach(Result res in allResults) {
                if(res.playerGames > res.opponentGames) {
                    if(!currentlyOnWinStreak) {  // OK, the lose streak has been broken
                        loseStreak = 0;
                        currentlyOnWinStreak = true;
                        winStreakStart_temp = res.date;  // in case this is the start of our best winning streak, let's take a note of the time
                    }
                    winStreak++;
                    if(winStreak > highWinStreak) {  // do we have a new winstreak PB?
                        highWinStreak = winStreak;
                        winStreakStart = winStreakStart_temp;
                        winStreakEnd = res.date;
                    }

                    if(res.ratingAdjustment > allResults[bestWin].ratingAdjustment) {
                        bestWin = loopCount;  // save the index of the current iteration, as this is our current best win
                    }
                    if((res.date > (DateTime.Today - new TimeSpan(365, 0, 0, 0))) && (res.ratingAdjustment > allResults[bestWinRecent].ratingAdjustment)) {
                        bestWinRecent = loopCount;
                    }
                }
                else {  // opponent won.  this will catch a draw as well, which i guess gets counted as a loss.  is a draw even possible?
                    if(currentlyOnWinStreak) {  // OK, the win streak is now over
                        winStreak = 0;
                        currentlyOnWinStreak = false;
                        loseStreakStart_temp = res.date;
                    }
                    loseStreak++;
                    if(loseStreak > highLoseStreak) {  // if we wanted to record the oldest lose/win streak, we'd use >=
                        highLoseStreak = loseStreak;
                        loseStreakStart = loseStreakStart_temp;
                        loseStreakEnd = res.date;
                    }

                    if(res.ratingAdjustment < allResults[worstLoss].ratingAdjustment) {
                        worstLoss = loopCount;
                    }

                    if((res.date > (DateTime.Today - new TimeSpan(365, 0, 0, 0))) && (res.ratingAdjustment < allResults[worstLossRecent].ratingAdjustment)) {
                        worstLossRecent = loopCount;
                    }
                }

                if(!isThreeMonthsDelta && (res.date < (DateTime.Today - new TimeSpan((3 * 30) - 1, 0, 0, 0)))) {  // just saying 30 days is a month..
                    isThreeMonthsDelta = true;  // only enter this if block once - the first time we find the right date, grab the matrix at that point.
                    threeMonthMatrix = res.rating;
                    Debug.Print("setting 3 month delta, the date is: " + res.date + " and the rating is: " + res.rating);
                }
                if(!isTwelveMonthsDelta && (res.date < (DateTime.Today - new TimeSpan(364, 0, 0, 0)))) {
                    isTwelveMonthsDelta = true;
                    twelveMothMatrix = res.rating;
                    Debug.Print("setting 12 month delta, the date is: " + res.date + " and the rating is: " + res.rating);
                }

                loopCount++;
            }

            LongestWinStreakLabel.Content = highWinStreak.ToString();
            if(winStreakStart.Year != 1900) {  // i hate using magic numbers, but i'm pretty sure we won't go back in time, so should be safe.
                LongestWinStreakLabel.ToolTip = "Most recent\r\nStreak start: " + winStreakStart.ToString("dd/MM/yyyy") + "\r\n" + "Streak end: " + winStreakEnd.ToString("dd/MM/yyyy");
            }

            LongestLoseStreakLabel.Content = highLoseStreak.ToString();
            if(loseStreakStart.Year != 1900) {
                LongestLoseStreakLabel.ToolTip = "Most recent\r\nStreak start: " + loseStreakStart.ToString("dd/MM/yyyy") + "\r\n" + "Streak end: " + loseStreakEnd.ToString("dd/MM/yyyy");
            }

            BestWinLabel.Content = allResults[bestWin].ratingAdjustment + " (" + allResults[bestWin].opponentName + ")";
            // this is pretty loose.  just assuming that their matrix went down by whatever ours went up by
            BestWinLabel.Content += "\r\nMatrix differential: " + ((allResults[bestWin].rating - allResults[bestWin].ratingAdjustment) - (allResults[bestWin].opponentRating + allResults[bestWin].ratingAdjustment));
            BestWinLabel.ToolTip = "This match happenend on: " + allResults[bestWin].date.ToString("dd/MM/yyyy") + "\r\nA negative differential means the oppenent's rating was higher than the player's.";

            WorstLossLabel.Content = allResults[worstLoss].ratingAdjustment + " (" + allResults[worstLoss].opponentName + ")";
            WorstLossLabel.Content += "\r\nMatrix differential: " + ((allResults[worstLoss].rating - allResults[worstLoss].ratingAdjustment) - (allResults[worstLoss].opponentRating + allResults[worstLoss].ratingAdjustment)); //+ getOpponentRatingAdjust(allResults[worstLoss]));
            WorstLossLabel.ToolTip = allResults[worstLoss].date.ToString("dd/MM/yyy");

            BestWinRecentLabel.Content = allResults[bestWinRecent].ratingAdjustment + " (" + allResults[bestWinRecent].opponentName + ")";
            BestWinRecentLabel.Content += "\r\nMatrix differential: " + ((allResults[bestWinRecent].rating - allResults[bestWinRecent].ratingAdjustment) - (allResults[bestWinRecent].opponentRating + allResults[bestWinRecent].ratingAdjustment));
            BestWinRecentLabel.ToolTip = allResults[bestWinRecent].date.ToString("dd/MM/yyyy");

            WorstLossRecentLabel.Content = allResults[worstLossRecent].ratingAdjustment + " (" + allResults[worstLossRecent].opponentName + ")";
            WorstLossRecentLabel.Content += "\r\nMatrix differential: " + ((allResults[worstLossRecent].rating - allResults[worstLossRecent].ratingAdjustment) - (allResults[worstLossRecent].opponentRating + allResults[worstLossRecent].ratingAdjustment));
            WorstLossRecentLabel.ToolTip = allResults[worstLossRecent].date.ToString("dd/MM/yyyy");

            MatrixDelta3Label.Content = (allResults.First().rating - threeMonthMatrix).ToString();
            MatrixDelta12Label.Content = (allResults.First().rating - twelveMothMatrix).ToString();
        }

        /// <summary>
        /// returns the rating adjustment that the opponent experienced after this match
        /// will return -100 if we can't find a corresponding match
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private double getOpponentRatingAdjust(Result match) {
            Debug.Print("GetOpponentRatingAdjust: comparing " + match.opponentID + " with " + PlayerNumber.Text);
            List<Result> opponentResults = MatrixInterface.getResults(match.opponentID);
            foreach (Result opponentRes in opponentResults) {
                Debug.Print(opponentRes.opponentID + " <> " + PlayerNumber.Text + "   |   " + opponentRes.date + " <> " + match.date);
                if ((opponentRes.opponentID == PlayerNumber.Text) && (opponentRes.date == match.date)) {
                    // OK, we've found the result!
                    return opponentRes.ratingAdjustment;
                }
            }
            return -100;
        }

        // trying to make Enter simulate clicking the Get stats button, but i don't think it worked
        private void PlayerNumber_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                //ButtonAutomationPeer peer = new ButtonAutomationPeer(GetStats_Button);
                //IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                //invokeProv.Invoke();

                GetStats_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void JBPbutton_Click(object sender, RoutedEventArgs e) {
            JoukbetPredictor JBPform = new JoukbetPredictor(this);
            JBPform.Show();
        }
    }
}
