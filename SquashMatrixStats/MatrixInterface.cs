using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;



namespace SquashMatrixStats {
    public class MatrixInterface {

        private string username = "";
        private string password = "";
        private bool loginSuccessful = false;
        private HttpClient client = new HttpClient();

        public void setUser(string user) {
            username = user;
        }
        public void setPass(string pass) {
            password = pass;
        }

        public async Task<List<Result>> getResults(string playerID) {
            Debug.Print("button clicked!");
            bool FoundTbody = false;
            bool FoundStartTR = false;
            bool hasNote = false;
            int lineCount = 0;

            string PageSource;
            string url = "http://www.squashmatrix.com/Home/PlayerResults/" + playerID + "?max=0";

            if(!loginSuccessful) {
                MessageBoxResult result = MessageBox.Show("Do you want to try and login first?", "Login before pulling data?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if(result == MessageBoxResult.Yes) {
                    loginToMatrix();
                }
            }

            PageSource = await client.GetStringAsync(url);
            Debug.Print("webpage gotted, " + url);

            if (PageSource == "Forbidden") {
                MessageBox.Show("You have been forbidden!  try again in an hour, or change your IP address, or login via the app!");
                Debug.Print("PageSource = " + PageSource);
                return null;
            }

            List<Result> allResults = new List<Result>();

            Result playerResult = new Result();

            using(StringReader reader = new StringReader(PageSource)) {
                string line;
                int count = 0;
                HTMLline HTMLlineObj;
                // OK this code parses through the web source, and splits out the lines for each match mentioned, creates a Result object, and adds it to a List or something
                while((line = reader.ReadLine()) != null) {
                    count++;
                    line = line.Trim();
                    //Debug.Print("\n\r looking at line: " + line);

                    // check for FoundTbody here, if true, then don't waste time performing a string compare as we've passed the Note section
                    if(!FoundTbody && line.Equals("<td><label for=\"Comments\">Note</label></td>")) {
                        // if this line exists, we have a Note column for some reason.  this seems to be optional
                        hasNote = true;
                    }
                    if(line.Equals("<tbody>", StringComparison.Ordinal)) {
                        FoundTbody = true;
                        continue;
                    }

                    if(line.Equals("<tr>", StringComparison.Ordinal) && FoundTbody) {
                        FoundStartTR = true;
                        playerResult = new Result();
                        continue;
                    }

                    // skip empty lines
                    /*if(line.Equals("<td class=\"unimportant\"></td>", StringComparison.Ordinal) || (line.Equals("<td></td>", StringComparison.Ordinal))) {
                        lineCount++;
                        continue;
                    }*/

                    HTMLlineObj = parseLine(line);

                    // ***** DATE *****
                    // ****************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 0)  // we're on the date line
                    {
                        //line = tdTrim(line);  // this should return just the date string without the <td> </td> crap
                        // let's make sure this is a date line
                        if(!HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            playerResult.date = DateTime.ParseExact(HTMLlineObj.tdValue.tdValue, "ddd dd/MM/yyyy", null);
                        }
                        lineCount++;
                        continue;
                    }

                    // ***** EVENT *****
                    // *****************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 1)  // we're on a event line
                    {
                        if(HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            playerResult.eventRegion = HTMLlineObj.tdValue.tdValue;
                        }
                        lineCount++;
                        continue;
                    }

                    // ***** DIVISION *****
                    // ********************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 2) {
                        if(HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            playerResult.division = HTMLlineObj.tdValue.tdValue;
                        }
                        lineCount++;
                        continue;
                    }

                    // ***** ROUND *****
                    // *****************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 3) {
                        if(HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            playerResult.round = HTMLlineObj.tdValue.tdValue;
                        }
                        lineCount++;
                        continue;
                    }

                    // ***** POSITION *****
                    // ********************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 4) {
                        if(HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            int position;
                            if(int.TryParse(HTMLlineObj.tdValue.tdValue, out position)) {
                                playerResult.position = position;
                            }
                            /*else {
                                Debug.Print("ERROR! could not parse position into an int: " + line);
                            }*/
                        }

                        lineCount++;
                        continue;
                    }

                    // ***** GAMES *****
                    // *****************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 5) {
                        if(!HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            string[] games = HTMLlineObj.tdValue.tdValue.Split('-');
                            if(games.Length == 2) {
                                int temp;
                                if(int.TryParse(games[0], out temp)) {
                                    playerResult.playerGames = temp;
                                }
                                else {
                                    Debug.Print("ERROR!  could not parse the first game: " + games[0]);
                                }
                                if(int.TryParse(games[1], out temp)) {
                                    playerResult.opponentGames = temp;
                                }
                                else {
                                    Debug.Print("ERROR!  could not parse the second game: " + games[1]);
                                }
                            }
                            else {
                                Debug.Print("ERROR!  parsing the games line didn't work out, there's not 2 games?? : " + line);
                            }
                            lineCount++;
                            continue;
                        }
                    }

                    // ***** POINTS *****
                    // ******************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 6) {
                        if(!HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            string[] points = HTMLlineObj.tdValue.tdValue.Split('-');
                            if(points.Length == 2) {
                                int temp;
                                if(int.TryParse(points[0], out temp)) {
                                    playerResult.playerPoints = temp;
                                }
                                else {
                                    Debug.Print("ERROR!  could not parse the first game: " + points[0]);
                                }
                                if(int.TryParse(points[1], out temp)) {
                                    playerResult.opponentPoints = temp;
                                }
                                else {
                                    Debug.Print("ERROR!  could not parse the second game: " + points[1]);
                                }
                            }
                            else {
                                Debug.Print("ERROR!  parsing the points line didn't work out, there's not 2 points?? : " + line);
                            }
                            lineCount++;
                            continue;
                        }
                    }

                    // ***** RATING ADJUSTMENT *****
                    // *****************************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 7) {
                        if(HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            double temp;
                            if(double.TryParse(HTMLlineObj.tdValue.tdValue, out temp)) {
                                playerResult.ratingAdjustment = temp;
                            }
                            else {
                                Debug.Print("ERROR! could not parse ratingAdjustment into a double: " + line);
                            }
                        }

                        lineCount++;
                        continue;
                    }

                    // ***** RATING *****
                    // ******************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 8) {
                        if(!HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            double temp;
                            if(double.TryParse(HTMLlineObj.tdValue.tdValue, out temp)) {
                                playerResult.rating = temp;
                            }
                            else {
                                Debug.Print("ERROR! could not parse rating into a double: " + line);
                            }
                        }
                        lineCount++;
                        continue;
                    }

                    // ***** OPPONENT NAME/ID *****
                    // ****************************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 9) {
                        if(!HTMLlineObj.hasClass && HTMLlineObj.tdValue.hasAHREF) {

                            //string opponentID = getBetween(line, "Player/", "\">");
                            string opponentID = HTMLlineObj.tdValue.AHREF.Substring(HTMLlineObj.tdValue.AHREF.IndexOf("Player/") + 7);
                            if(opponentID.Length > 0) {
                                playerResult.opponentID = opponentID;
                            }
                            else {
                                Debug.Print("ERROR!  couldn't parse the player ID out of: " + line);
                            }

                            playerResult.opponentName = HTMLlineObj.tdValue.tdValue;
                        }

                        lineCount++;
                        continue;
                    }

                    // ***** OPPONENT'S RATING *****
                    // *****************************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 10) {
                        if(!HTMLlineObj.hasClass && !HTMLlineObj.tdValue.hasAHREF) {
                            double temp;
                            if(double.TryParse(HTMLlineObj.tdValue.tdValue, out temp)) {
                                playerResult.opponentRating = temp;
                            }
                            else {
                                Debug.Print("ERROR! could not parse opponent rating into a double: " + line);
                            }
                        }
                        lineCount++;
                        if(!hasNote) lineCount++;  // if there is no Note column, then advance to the match results
                        continue;
                    }

                    // ***** NOTES *****
                    // *****************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 11) {
                        playerResult.note = HTMLlineObj.tdValue.tdValue;
                        lineCount++;
                        continue;
                    }

                    // ***** MATCH RESULTS *****
                    // *************************
                    if(HTMLlineObj.isTD && FoundStartTR && lineCount == 12) {
                        if(HTMLlineObj.hasClass && HTMLlineObj.tdValue.hasAHREF) {

                            //string opponentID = getBetween(line, "Player/", "\">");
                            string matchID = HTMLlineObj.tdValue.AHREF.Substring(HTMLlineObj.tdValue.AHREF.IndexOf("Match/") + 6);
                            if(matchID.Length > 0) {
                                playerResult.matchResultID = matchID;
                            }
                            else {
                                Debug.Print("ERROR!  couldn't parse the match ID out of: " + line);
                            }
                        }
                        lineCount++;
                        continue;
                    }



                    if(line.StartsWith("</tr>") && FoundTbody) {
                        //Debug.Print("adding " + playerResult.date.ToString() + " result against " + playerResult.opponentName + " with a score of " + playerResult.playerGames.ToString() + " to " + playerResult.opponentGames.ToString() + " to list");
                        lineCount = 0;
                        FoundStartTR = false;
                        allResults.Add(playerResult);
                    }


                    /*if (count > 30) {
                        break;
                    }*/


                }  // end while
                return allResults;
            }
        }

        // this doesn't work.. or does it?
        private async void loginToMatrix() {

            Login LoginForm = new Login(this);
            LoginForm.ShowDialog();

        Dictionary<string, string> values = new Dictionary<string, string> {
            { "UserName", username },
            { "Password", password },
            { "RememberMe", "false" }
        };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://www.squashmatrix.com/Account/LogOn", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if(responseString.Contains("The details you provided are incorrect")) {
                MessageBox.Show("user/pass incorrect!  will continue to load data unauthenticated, but you will likely be blocked pretty quickly until you put a correct user/pass in");
                client = new HttpClient();
            }
            else {
                loginSuccessful = true;
            }
        }


        /// <summary>
        /// this class stores all attributes of an line parsed from the website
        /// </summary>
        private class HTMLline {
            private bool _isTD = false;
            private bool _hasClass = false;
            private string _classValue = "";
            private tdValueLine _tdValue;

            public bool isTD {
                get {
                    return _isTD;
                }
                set {
                    _isTD = value;
                    //Debug.Print("--isTD is: " + _isTD.ToString());
                }
            }

            public bool hasClass {
                get {
                    return _hasClass;
                }
                set {
                    _hasClass = value;
                    //Debug.Print("--hasClass is : " + _hasClass.ToString());
                }
            }

            public string classValue {
                get {
                    return _classValue;
                }
                set {
                    _classValue = value;
                    //Debug.Print("--classValue is : " + _classValue);
                }
            }

            public tdValueLine tdValue {
                get {
                    return _tdValue;
                }
                set {
                    _tdValue = value;
                }
            }
        }

        private class tdValueLine {
            private bool _hasAHREF = false;
            private string _AHREF = "";
            private string _tdValue = "";

            public bool hasAHREF {
                get {
                    return _hasAHREF;
                }
                set {
                    _hasAHREF = value;
                    //Debug.Print("--hasAHREF is : " + _hasAHREF.ToString());
                }
            }

            public string AHREF {
                get {
                    return _AHREF;
                }
                set {
                    _AHREF = value;
                    //Debug.Print("--AHREF is : " + _AHREF);
                }
            }

            // if hasAHREF is true, then the tdValue becomes the a href label
            public string tdValue {
                get {
                    return _tdValue;
                }
                set {
                    _tdValue = value;
                    //Debug.Print("--tdValue is : " + _tdValue);
                }
            }
        }


        /// <summary>
        /// this method converts a line of HTML into an HTMLline object
        /// need to be able to parse the following possible lines:
        ///  <td class="negativevalue">-1.51</td>
        ///  <td>249.05</td>
        ///  <td></td>
        ///  <td><a href = "/Home/Player/54115" > Jason Budding</a></td>
        ///  <td class="unimportant"><a href = "/Home/Match/1036897" > Match Results</a></td>
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private HTMLline parseLine(string line) {
            // let's try and parse this thing.  just.. geeing myself up here
            line = line.Trim();
            HTMLline HTMLlineObj = new HTMLline();
            tdValueLine tdValueLineObj = new tdValueLine();

            if(line.StartsWith("<td")) {
                HTMLlineObj.isTD = true;
                line = line.Substring(3);  // shave the line, we don't need the <td part anymore
                if(line[0] == '>') { // ie the line starts with <td>, there's no class attributes, we just get straight to the td value.  we'll get to that later
                    HTMLlineObj.hasClass = false;
                    line = line.Substring(1);  // shave
                }
                else {  // assume the line must have a class value
                    if(line.StartsWith(" class=\"")) {
                        HTMLlineObj.hasClass = true;
                        int equalLoc = line.IndexOf('=') + 2;  // i guess +2 , 1 for it being a 0 based array, and 1 for us wanting to move to the character after the "
                        int greaterThanLoc = line.IndexOf("\">");
                        HTMLlineObj.classValue = line.Substring(equalLoc, greaterThanLoc - equalLoc);
                        line = line.Substring(greaterThanLoc + 2);  // need to move 2 characters forward, so we don't keep "> in the string.
                    }
                    else {
                        Debug.Print("ERROR - parsing HTML td didn't end in >, but didn't have a class: " + line);
                    }
                }
                // ok we have parsed the inner <td> block, now to get to the meat of the td value
                if(line.StartsWith("<a ")) {
                    tdValueLineObj.hasAHREF = true;
                    line = line.Substring(line.IndexOf("=\"") + 2);  // this should trim the '<a href="' from the start of the line string
                    tdValueLineObj.AHREF = line.Substring(0, line.IndexOf("\">"));
                    tdValueLineObj.tdValue = line.Substring(line.IndexOf("\">") + 2, line.IndexOf("</a>") - line.IndexOf("\">") - 2);
                }
                else {
                    tdValueLineObj.hasAHREF = false;
                    tdValueLineObj.tdValue = line.Substring(0, line.Length - 5);  // "</td>".Length = 4 , we,re asumming the line ends here with a </td>
                }
                HTMLlineObj.tdValue = tdValueLineObj;

            }
            else {
                //Debug.Print("We're trynig to parse an HTML line that's not a TD.. bailing: " + line);
            }
            return HTMLlineObj;
        }

        public class squashPlayer {
            private List<string> _clubs;
            private List<string> _teams;
            private List<Result> _results;

            public string name { get; set; }
            public decimal rating { get; set; }
            public List<string> teams {
                get { return _teams; }
                set { _teams = value; }
            }
            public void addTeam (string team) {
                _teams.Add(team);
            }
            public string division { get; set; }
            public string eventRegion { get; set; }
            public string group { get; set; }
            public List<string> clubs {
                get { return _clubs; }
                set { _clubs = value; }
            }
            public void addClub(string club) {
                _clubs.Add(club);
            }
            public BitmapImage picture{ get; set; }
            public List<Result> results {
                get { return _results; }
                set { _results = value; }
            }
            public void addResult (Result res) {
                _results.Add(res);
            }
        }

        // pull from the player summary page and grab as much data as we can
        public async Task<double> parsePlayerSummary(string playerID) {

            string PageSource;
            string url = "http://squashmatrix.com/Home/Player/" + playerID;

            if(!loginSuccessful) {
                MessageBoxResult result = MessageBox.Show("Do you want to try and login first?", "Login before pulling data?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if(result == MessageBoxResult.Yes) {
                    loginToMatrix();
                }
            }

            PageSource = await client.GetStringAsync(url);

            using(StringReader reader = new StringReader(PageSource)) {
                string line;
                bool nextLine = false;
                HTMLline HTMLlineObj;

                while((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    if (line == "<td><label for=\"Rating\">Rating</label></td>") {  // this line actually shows up twice, we only care about the first instance
                        nextLine = true;
                        continue;
                    }
                    else if (nextLine && line != "<td><label for=\"Rating\">Rating</label></td>") {
                        HTMLlineObj = parseLine(line);

                        double temp;
                        if(double.TryParse(HTMLlineObj.tdValue.tdValue, out temp)) {
                            Debug.Print("getCurrentRating: player " + playerID + " has a rating of " + temp + " and decimalised: " + (decimal)temp);
                            return temp;
                        }
                        else {
                            Debug.Print("ERROR! could not parse current rating into an double: " + line);
                            return 0;
                        }
                    }
                }
            }
            return 0;  // if we don't match somehow..
        }
    }
}
