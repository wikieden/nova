﻿using MongoDB.Bson;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using System.Windows.Media;

namespace ssi
{
    internal class DatabaseHandler
    {
        private IMongoDatabase database;
        private MongoClient mongo;
        private string connectionstring = "mongodb://127.0.0.1:27017";

        private string GARBAGELABEL = "GARBAGE";
        private Color GARBAGECOLOR = Colors.Black;

        public DatabaseHandler(string constr)
        {
            this.connectionstring = constr;
        }

        public int checkAuth(string dbuser, string db = "admin")
        {
            //4 = root
            //3 = admin
            //2 = write
            //1 = read
            //0 = notauthorized

            int auth = 0;
            try
            {
                mongo = new MongoClient(connectionstring);
                var adminDB = mongo.GetDatabase("admin");
                var cmd = new BsonDocument("usersInfo", dbuser);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if ((roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && roles[i]["db"] == db || (roles[i]["role"].ToString() == "userAdminAnyDatabase"  || roles[i]["role"].ToString() == "dbAdminAnyDatabase")) && auth <= 4) { auth = 4; }
                    else if ((roles[i]["role"].ToString() == "dbAdmin" && roles[i]["db"] == db) && auth <= 3) { auth = 3; }
                    else if ((roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && roles[i]["db"] == db || roles[i]["role"].ToString() == "read" && roles[i]["db"] == db) && auth <= 2) { auth = 2; }
                    else if ((roles[i]["role"].ToString() == "readAnyDatabase") && auth <= 1) { auth = 1; }


                    //edit/add more roles if you want to change security levels
                }
            }
            catch(Exception e)
            {
               
               
            }

            return auth;
        }



        //public void sendmail(string adressto, string subject, string body)
        //{
        //    //dummy code, need some logic but hey, automated notifications
        //    MailMessage objeto_mail = new MailMessage();
        //    SmtpClient client = new SmtpClient();
        //    client.Port = 25;
        //    client.Host = "smtp.internal.mycompany.com";
        //    client.Timeout = 10000;
        //    client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //    client.UseDefaultCredentials = false;
        //    client.Credentials = new System.Net.NetworkCredential("user", "Password");
        //    objeto_mail.From = new MailAddress("from@server.com");
        //    objeto_mail.To.Add(new MailAddress(adressto));
        //    objeto_mail.Subject = subject;
        //    objeto_mail.Body = body;
        //    client.Send(objeto_mail);
        //}


        public string LoadRoles(string db, AnnoTier tier)
        {
            string role = null;
            List<string> roles = new List<string>();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("Roles");

            var sessions = collection.Find(_ => true).ToList();

            foreach (var document in sessions)
            {
                if (document["isValid"].AsBoolean == true) roles.Add(document["name"].ToString());
            }

           
            int auth = checkAuth(Properties.Settings.Default.MongoDBUser, db);
            bool hasauth = false;
            if (auth > 3) hasauth = true;

            string name = "New track";
            if (tier != null) name = tier.AnnoList.Name;

            DatabaseSelectionWindow dbw = new DatabaseSelectionWindow(roles, hasauth, "Tier: " + name + ". Who is annotated? ", "Roles");
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.DialogResult == true)
            {
                role = dbw.Result().ToString();
            }

            return role;
        }

        public string SelectAnnotationScheme(string db, AnnoTier tier, AnnoScheme.TYPE type = AnnoScheme.TYPE.DISCRETE)
        {
            string annotype = null;
            List<string> AnnotationSchemes = new List<string>();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("AnnotationSchemes");

            var sessions = collection.Find(_ => true).ToList();

            foreach (var document in sessions)
            {
                if (document["isValid"].AsBoolean == true)
                {
                    if ((type == AnnoScheme.TYPE.DISCRETE && document["type"].ToString() == "DISCRETE")) AnnotationSchemes.Add(document["name"].ToString());
                    else if  (type == AnnoScheme.TYPE.FREE && (document["type"].ToString() == "FREE")) AnnotationSchemes.Add(document["name"].ToString());
                    else if (type == AnnoScheme.TYPE.CONTINUOUS && document["type"].ToString() == "CONTINUOUS") AnnotationSchemes.Add(document["name"].ToString());
                }
            }

            int auth = checkAuth(Properties.Settings.Default.MongoDBUser, db);
            bool hasauth = false;
            if (auth > 2) hasauth = true;

            string name = "New Track";
            if (tier != null) name = tier.AnnoList.Name;

            DatabaseSelectionWindow dbw = new DatabaseSelectionWindow(AnnotationSchemes, hasauth, "Tier: " + name + ". What is annotated? ", "AnnotationSchemes", type, true, tier);
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.DialogResult == true)
            {
                annotype = dbw.Result().ToString();
            }
            return annotype;
        }

        public AnnoScheme GetAnnotationScheme(string annoName, AnnoScheme.TYPE annoType)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);

            AnnoScheme scheme = new AnnoScheme();
            scheme.Type = annoType;

            var annoSchemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
            var builder = Builders<BsonDocument>.Filter;

            FilterDefinition<BsonDocument> annoSchemeFilter = builder.Eq("name", annoName) & builder.Eq("type", annoType.ToString());
            BsonDocument annoSchemeDocument = null;
            try
            {
                annoSchemeDocument = annoSchemes.Find(annoSchemeFilter).Single();
                if (annoSchemeDocument["type"].ToString() == annoType.ToString())
                { 
                    scheme.Name = annoSchemeDocument["name"].ToString();
                    if (scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                    {
                        scheme.MinScore = annoSchemeDocument["min"].ToDouble();
                        scheme.MaxScore = annoSchemeDocument["max"].ToDouble();
                        scheme.SampleRate = annoSchemeDocument["sr"].ToDouble();
                        scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["min_color"].ToString());
                        scheme.MaxOrForeColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["max_color"].ToString());
                    }
                    else if (scheme.Type == AnnoScheme.TYPE.DISCRETE)
                    {
                        scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["color"].ToString());
                        BsonArray schemeLabelsArray = annoSchemeDocument["labels"].AsBsonArray;
                        string SchemeLabel = "";
                        string SchemeColor = "#000000";
                        for (int j = 0; j < schemeLabelsArray.Count; j++)
                        {
                            try
                            {
                                if (schemeLabelsArray[j]["isValid"].AsBoolean == true)
                                {
                                    SchemeLabel = schemeLabelsArray[j]["name"].ToString();
                                    SchemeColor = schemeLabelsArray[j]["color"].ToString();
                                    AnnoScheme.Label lcp = new AnnoScheme.Label(schemeLabelsArray[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemeLabelsArray[j]["color"].ToString()));
                                    scheme.Labels.Add(lcp);
                                }
                            }
                            catch
                            {
                                SchemeLabel = schemeLabelsArray[j]["name"].ToString();
                                SchemeColor = schemeLabelsArray[j]["color"].ToString();
                                AnnoScheme.Label lcp = new AnnoScheme.Label(schemeLabelsArray[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemeLabelsArray[j]["color"].ToString()));
                                scheme.Labels.Add(lcp);
                            }

                        }
                    }
                    else if (scheme.Type == AnnoScheme.TYPE.FREE)
                    {
                        scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["color"].ToString());
                    }
                }
            }
            catch(Exception ex)
            {   
                MessageTools.Warning(ex.ToString());            
            }
           
            return scheme;
        }

        public string StoreToDatabase(string db, string session, string dbuser, AnnoTier annoTrack, List<DatabaseMediaInfo> loadedDBmedia = null, bool isfin = false)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            string annotator = null;
            string originalannotator = annoTrack.AnnoList.Annotator;

            BsonArray labels = new BsonArray();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            var annotations = database.GetCollection<BsonDocument>("Annotations");
            var annotators = database.GetCollection<BsonDocument>("Annotators");
            var sessions = database.GetCollection<BsonDocument>("Sessions");
            var roles = database.GetCollection<BsonDocument>("Roles");
            var medias = database.GetCollection<BsonDocument>("Media");
            var annotationschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");

            string lowb = "", highb = "";
            if (!annoTrack.isDiscreteOrFree)
            {
                lowb = annoTrack.AnnoList.Scheme.MinScore.ToString();
                highb = annoTrack.AnnoList.Scheme.MaxScore.ToString();
            }

            ObjectId roleid;

            if (annoTrack.AnnoList.Role == null || annoTrack.AnnoList.Role == annoTrack.AnnoList.Name)
            {
                annoTrack.AnnoList.Role = LoadRoles(db, annoTrack);
                if (annoTrack.AnnoList.Role == null)
                {
                    return null;
                }
            }

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", annoTrack.AnnoList.Role);
            var roledb = roles.Find(filter).ToList();
            if (roledb.Count > 0)
            {
                roleid = roledb[0].GetValue(0).AsObjectId;
                var update = Builders<BsonDocument>.Update.Set("isValid", true);
                roles.UpdateOne(filter, update);
            }
            else
            {
                BsonDocument b = new BsonDocument();
                BsonElement n = new BsonElement("name", annoTrack.AnnoList.Name);
                BsonElement m = new BsonElement("isValid", true);

                b.Add(n);
                b.Add(m);
                roles.InsertOne(b);
                roleid = b.GetValue(0).AsObjectId;
            }

            BsonDocument annotatordoc = new BsonDocument();

            //We could choose here if we want to overwrite other peoples annotations. For now, we we might 
            //want to overwrite automatically created annotations and own annotations only. Here are two examples to e.g. let an admin allow to change existing annotations or a user called "system

            // if(checkAuth(dbuser) > 3)
            if (dbuser == "system")
            {

                if (annoTrack.AnnoList.Annotator == null && annoTrack.AnnoList.AnnotatorFullName != null)
                {
                    ObjectId annotatid = GetObjectID(database, "Annotators", "fullname", annoTrack.AnnoList.AnnotatorFullName);
                    annoTrack.AnnoList.Annotator = FetchDBRef(database, "Annotators", "name", annotatid);
                }
                else if (annoTrack.AnnoList.Annotator == null && annoTrack.AnnoList.AnnotatorFullName == null)
                {
                    annoTrack.AnnoList.Annotator = dbuser;
                }

                if (annoTrack.AnnoList.AnnotatorFullName == null)
                {
                    try
                    {
                        ObjectId annotatid = GetObjectID(database, "Annotators", "name", annoTrack.AnnoList.Annotator);
                        annoTrack.AnnoList.AnnotatorFullName = FetchDBRef(database, "Annotators", "fullname", annotatid);
                    }
                    catch
                    {
                        annoTrack.AnnoList.AnnotatorFullName = Properties.Settings.Default.Annotator;
                    }
                }
            }

            else if (!(annoTrack.AnnoList.AnnotatorFullName == "RMS" || annoTrack.AnnoList.AnnotatorFullName == "Median"))
            {
                try
                {
                    annoTrack.AnnoList.Annotator = dbuser;
                    ObjectId annotatid = GetObjectID(database, "Annotators", "name", dbuser);
                    annoTrack.AnnoList.AnnotatorFullName = FetchDBRef(database, "Annotators", "fullname", annotatid);
                }
                catch
                {
                    annoTrack.AnnoList.AnnotatorFullName = Properties.Settings.Default.Annotator;
                }

            }
            else annoTrack.AnnoList.Annotator = annoTrack.AnnoList.AnnotatorFullName;

            annotator = annoTrack.AnnoList.AnnotatorFullName;

            //  if (!(a.AnnoList.Annotator == null || a.AnnoList.Annotator == dbuser || a.AnnoList.Annotator == "RMS" || a.AnnoList.Annotator == "Median")) break; //?? Not called at the moment

            BsonElement annotatorname = new BsonElement("name", annoTrack.AnnoList.Annotator);
            BsonElement annotatornamefull = new BsonElement("fullname", annoTrack.AnnoList.AnnotatorFullName);

            annotatordoc.Add(annotatorname);
            annotatordoc.Add(annotatornamefull);

            var filterannotator = builder.Eq("name", annoTrack.AnnoList.Annotator);
            UpdateOptions uoa = new UpdateOptions();
            uoa.IsUpsert = true;
            var resann = annotators.ReplaceOne(filterannotator, annotatordoc, uoa);
            ObjectId annotatoroID = annotators.Find(filterannotator).Single()["_id"].AsObjectId;

            ObjectId sessionID;

            var filtersid = builder.Eq("name", session);
            var ses = sessions.Find(filtersid).Single();
            sessionID = ses.GetValue(0).AsObjectId;

            ObjectId annotid;
            string annotype = null;
            if (annoTrack.AnnoList.Scheme != null) annotype = annoTrack.AnnoList.Scheme.Name;

            if (annotype == null)
            {
                annotype = SelectAnnotationScheme(db, annoTrack, annoTrack.AnnoList.Scheme.Type);
            }

            if (annotype == null)
            {
                return null;
            }
            var filtera = builder.Eq("name", annotype);
            var annotdb = annotationschemes.Find(filtera).ToList();
            annotid = annotdb[0].GetValue(0).AsObjectId;
            var annoschemetypedb = annotdb[0]["type"];
            var update2 = Builders<BsonDocument>.Update.Set("isValid", true);
            annotationschemes.UpdateOne(filter, update2);

            BsonElement user = new BsonElement("annotator_id", annotatoroID);
            BsonElement role = new BsonElement("role_id", roleid);
            BsonElement annot = new BsonElement("scheme_id", annotid);
            BsonElement sessionid = new BsonElement("session_id", sessionID);
            BsonElement isfinished = new BsonElement("isFinished", isfin);
            BsonElement date = new BsonElement("date", new BsonDateTime(DateTime.Now));
            BsonDocument document = new BsonDocument();

            BsonArray media = new BsonArray();

            if (loadedDBmedia != null)
            {
                foreach (DatabaseMediaInfo dmi in loadedDBmedia)
                {
                    BsonDocument mediadocument = new BsonDocument();
                    ObjectId mediaid;

                    var filtermedia = builder.Eq("name", dmi.filename) & builder.Eq("session_id", sessionID);
                    var mediadb = medias.Find(filtermedia).ToList();

                    if (mediadb.Count > 0)
                    {
                        mediaid = mediadb[0].GetValue(0).AsObjectId;

                        BsonElement media_id = new BsonElement("media_id", mediaid);
                        mediadocument.Add(media_id);
                        media.Add(mediadocument);
                    }
                }
            }

            BsonArray data = new BsonArray();
            document.Add(sessionid);
            document.Add(user);
            document.Add(role);
            document.Add(annot);
            document.Add(date);
            document.Add(isfinished);
            document.Add("media", media);

            if (annoTrack != null)
            {
                if (annoschemetypedb == "DISCRETE")
                {
                    BsonArray Labels = annotdb[0]["labels"].AsBsonArray;
                    int index = 0;
                    for (int i = 0; i < annoTrack.AnnoList.Count; i++)
                    {
                        for (int j = 0; j < Labels.Count; j++)
                        {
                            if (annoTrack.AnnoList[i].Label == Labels[j]["name"].ToString())
                            {
                                index = Labels[j]["id"].AsInt32;
                                data.Add(new BsonDocument { { "from", annoTrack.AnnoList[i].Start }, { "to", annoTrack.AnnoList[i].Stop }, { "id", index }, { "conf", annoTrack.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                                break;
                            }
                            else if (annoTrack.AnnoList[i].Label == GARBAGELABEL)
                            {
                                data.Add(new BsonDocument { { "from", annoTrack.AnnoList[i].Start }, { "to", annoTrack.AnnoList[i].Stop }, { "id", -1 }, { "conf", annoTrack.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                                break;
                            }
                        }
                    }
                }
                else if (annoschemetypedb == "FREE")
                {
                    for (int i = 0; i < annoTrack.AnnoList.Count; i++)
                    {
                        data.Add(new BsonDocument { { "from", annoTrack.AnnoList[i].Start }, { "to", annoTrack.AnnoList[i].Stop }, { "name", annoTrack.AnnoList[i].Label }, { "conf", annoTrack.AnnoList[i].Confidence } });
                    }
                }
                else if (annoschemetypedb == "CONTINUOUS")
                {
                    for (int i = 0; i < annoTrack.AnnoList.Count; i++)
                    {
                        data.Add(new BsonDocument { { "score", annoTrack.AnnoList[i].Label }, { "conf", annoTrack.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                    }
                }

                document.Add("labels", data);
            }

            var filter2 = builder.Eq("scheme_id", annotid) & builder.Eq("role_id", roleid) & builder.Eq("annotator_id", annotatoroID) & builder.Eq("session_id", sessionID);

            ObjectId annoid = new ObjectId();
            var res = annotations.Find(filter2).ToList();
            if (res.Count > 0)
            {
                annoid = res[0].GetElement(0).Value.AsObjectId;
            }



            UpdateOptions uo = new UpdateOptions();
            uo.IsUpsert = true;

          

            bool islocked = false;
            try
            {
                var checklock = annotations.Find(filter2).Single();
                islocked = checklock["isLocked"].AsBoolean;

            }
            catch (Exception ex) { }

            if (!islocked)
            {
                var check = annotations.Find(filter2).ToList();
                if (check.Count > 0 && Properties.Settings.Default.DatabaseAskBeforeOverwrite && originalannotator != Properties.Settings.Default.MongoDBUser)
                {
                   MessageBoxResult mbres =  MessageBox.Show("Annotation #" + annoTrack.AnnoList.Role+ " #" + annoTrack.AnnoList.Scheme.Name + " #" +
                       annoTrack.AnnoList.AnnotatorFullName + " already exists, do you want to overwrite it?", "Attention", MessageBoxButton.YesNo);

                    if (mbres == MessageBoxResult.Yes)
                    {
                        annotations.ReplaceOne(filter2, document, uo);
                    }
                    else return null;


                }

                var result = annotations.ReplaceOne(filter2, document, uo);
            }
            else {
                MessageBox.Show("Annotaion is locked and therefore can't be overwritten");
                    return null;
                    };


            return annotator;
        }

        public IMongoDatabase GetDatabase()
        {
            return database;
        }



        public List<AnnoList> LoadFromDatabase(System.Collections.IList collections, string db, string session, string dbuser)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            var collection = database.GetCollection<BsonDocument>("Annotations");
            var roles = database.GetCollection<BsonDocument>("Roles");
            var annoschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");

            List<AnnoList> l = new List<AnnoList>();

            foreach (DatabaseAnno s in collections)
            {
                BsonElement value;
                AnnoList annoList = new AnnoList();

                ObjectId roleid = GetObjectID(database, "Roles", "name", s.Role);
                string roledb = FetchDBRef(database, "Roles", "name", roleid);

                ObjectId annotid = GetObjectID(database, "AnnotationSchemes", "name", s.AnnoScheme);
                string annotdb = FetchDBRef(database, "AnnotationSchemes", "name", annotid);

                ObjectId annotatid = GetObjectID(database, "Annotators", "fullname", s.AnnotatorFullname);
                string annotatdb = FetchDBRef(database, "Annotators", "name", annotatid);
                string annotatdbfn = FetchDBRef(database, "Annotators", "fullname", annotatid);
                annoList.Annotator = annotatdb;
                annoList.AnnotatorFullName = annotatdbfn;

                ObjectId sessionid = GetObjectID(database, "Sessions", "name", session);
                string sessiondb = FetchDBRef(database, "Sessions", "name", sessionid);

                var builder = Builders<BsonDocument>.Filter;

                var filterscheme = builder.Eq("_id", annotid);
                var result = collection.Find(filterscheme);
                var annosch = annoschemes.Find(filterscheme).Single();

                var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);
                var documents = collection.Find(filter).ToList();

                annoList.Scheme = new AnnoScheme();
                if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == AnnoScheme.TYPE.DISCRETE.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.DISCRETE;
                }
                else if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == AnnoScheme.TYPE.FREE.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.FREE;
                }
                else if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == AnnoScheme.TYPE.CONTINUOUS.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.CONTINUOUS;
                }

                annoList.Role = roledb;
                annoList.Name = "#" +annoList.Role + " #" + annotdb + " #" + annotatdbfn;

                annoList.Scheme.Name = annosch["name"].ToString();
                var annotation = documents[0]["labels"].AsBsonArray;
                if (annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    if (annosch.TryGetElement("min", out value)) annoList.Scheme.MinScore = double.Parse(annosch["min"].ToString());
                    if (annosch.TryGetElement("max", out value)) annoList.Scheme.MaxScore = double.Parse(annosch["max"].ToString());
                    if (annosch.TryGetElement("sr", out value)) annoList.Scheme.SampleRate = double.Parse(annosch["sr"].ToString());

                    if (annosch.TryGetElement("min_color", out value)) annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annosch["min_color"].ToString());
                    if (annosch.TryGetElement("max_color", out value)) annoList.Scheme.MaxOrForeColor = (Color)ColorConverter.ConvertFromString(annosch["max_color"].ToString());

                    annoList.Scheme.MinScore = annoList.Scheme.MinScore;
                    annoList.Scheme.MaxScore = annoList.Scheme.MaxScore;
                    annoList.Scheme.SampleRate = annoList.Scheme.SampleRate;

                    for (int i = 0; i < annotation.Count; i++)
                    {
                        string label = annotation[i]["score"].ToString();
                        string confidence = annotation[i]["conf"].ToString();
                        double start = i * ((1000.0 / annoList.Scheme.SampleRate) / 1000.0);
                        double dur = (1000.0 / annoList.Scheme.SampleRate) / 1000.0;

                        AnnoListItem ali = new AnnoListItem(start, dur, label, "", Colors.Black, double.Parse(confidence));

                        annoList.Add(ali);
                    }
                }
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annosch["color"].ToString());

                    annoList.Scheme.Labels = new List<AnnoScheme.Label>();

                    BsonArray schemelabels = annosch["labels"].AsBsonArray;

                    for (int j = 0; j < schemelabels.Count; j++)
                    {
                        //in case flag is set, if not ignore isValid
                        try
                        {
                            if (schemelabels[j]["isValid"].AsBoolean == true) annoList.Scheme.Labels.Add(new AnnoScheme.Label(schemelabels[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString())));
                        }
                        catch
                        {
                            annoList.Scheme.Labels.Add(new AnnoScheme.Label(schemelabels[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString())));
                        }
                    }

                    annoList.Scheme.Labels.Add(new AnnoScheme.Label(GARBAGELABEL, GARBAGECOLOR));

                    for (int i = 0; i < annotation.Count; i++)
                    {
                        string SchemeLabel = "";
                        Color SchemeColor = Colors.Black;
                        bool idfound = false;
                        for (int j = 0; j < schemelabels.Count; j++)
                        {
                            if (annotation[i]["id"].AsInt32 == schemelabels[j]["id"].AsInt32)
                            {
                                SchemeLabel = schemelabels[j]["name"].ToString();
                                SchemeColor = (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString());
                                idfound = true;
                                break;
                            }
                        }


                        if (annotation[i]["id"].AsInt32 == -1 || idfound == false)
                        {
                            SchemeLabel = GARBAGELABEL;
                            SchemeColor = GARBAGECOLOR;
                        }

                        double start = double.Parse(annotation[i]["from"].ToString());
                        double stop = double.Parse(annotation[i]["to"].ToString());
                        double duration = stop - start;
                        string label = SchemeLabel;
                        string confidence = annotation[i]["conf"].ToString();

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", SchemeColor, double.Parse(confidence));
                        annoList.AddSorted(ali);
                    }
                }
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annosch["color"].ToString());

                    for (int i = 0; i < annotation.Count; i++)
                    {
                        double start = double.Parse(annotation[i]["from"].ToString());
                        double stop = double.Parse(annotation[i]["to"].ToString());
                        double duration = stop - start;
                        string label = annotation[i]["name"].ToString();
                        string confidence = annotation[i]["conf"].ToString();

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", Colors.Black, double.Parse(confidence));
                        annoList.AddSorted(ali);
                    }
                }

                annoList.LoadedFromDB = true;
                l.Add(annoList);
                annoList = null;
            }

            return l;
        }

        public string FetchDBRef(IMongoDatabase database, string collection, string attribute, ObjectId reference)
        {
            string output = "";
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq("_id", reference);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0)
            {
                output = result[0][attribute].ToString();
            }

            return output;
        }

        public ObjectId GetObjectID(IMongoDatabase database, string collection, string value, string attribute)
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq(value, attribute);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0) id = result[0].GetValue(0).AsObjectId;

            return id;
        }
    }

    public class DatabaseAnno
    {
        public ObjectId Id { get; set; }

        public string Role { get; set; }

        public string AnnoScheme { get; set; }

        public string Annotator { get; set; }

        public string AnnotatorFullname { get; set; }

        public string Session { get; set; }

        public bool IsFinished { get; set; }

        public bool IsLocked { get; set; }
        
        public bool IsOwner { get; set; }

        public string Date { get; set; }

        public ObjectId OID { get; set; }
    }

    public class DatabaseSession
    {
        public string Name { get; set; }

        public string Language { get; set; }

        public string Location { get; set; }

        public string Date { get; set; }

        public ObjectId OID { get; set; }
    }

    public class DatabaseMediaInfo
    {
        public string connection;
        public string ip;
        public string folder;
        public string login;
        public string pw;
        public string filepath;
        public string filename;
        public string requiresauth;
        public string role;
        public string subject;
        public string mediatype;
        public string session;
    }
}