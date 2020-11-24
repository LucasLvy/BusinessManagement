using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System.Globalization;
using System.Xml;

namespace BDD_td5
{

    class Program
    {
        static void Main(string[] args)
        {

            string connectionString = "SERVER=localhost;PORT=3306;DATABASE=cooking;UID=root;PASSWORD=DCshoes987;";

            MySqlConnection connection = new MySqlConnection(connectionString);
            bool connecte = false;
            int id = 0;
            while (!connecte)
            {
                Console.Clear();
                Console.WriteLine("Souhaitez-vous : \n" +
                              "1. Vous connecter \n" +
                              "2. Créer un compte\n" +
                              "3. Accéder à l'interface d'administration"); ;
                switch (Convert.ToInt32(Console.ReadLine()))
                {
                    case 1:
                        id = Login(connection);
                        connecte = id != 0;
                        if (connecte) { Interface(connection, id); }
                        break;
                    case 2:
                        CreerClient(connection);
                        break;
                    case 3:
                        InterfaceAdmin(connection);
                        break;
                }

                // demo(connection);

            }
        }
        /// <summary>
        /// Affiche toutes les recettes disponibles
        /// </summary>
        /// <param name="connexion"></param>
        static void affichageRecettes(MySqlConnection connexion)
        {
            connexion.Open();

            MySqlCommand command = connexion.CreateCommand();
            command.CommandText = "SELECT nom_recette,type, descriptif,prix_vente recette FROM recette;";

            MySqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string currentRowAsString = "";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string valueAsString = reader.GetValue(i).ToString();
                    currentRowAsString += valueAsString + ", ";
                }
                Console.WriteLine(currentRowAsString);
            }
            Console.WriteLine("Appuyez sur une touche pour continuer");

            Console.ReadKey();
            connexion.Close();
        }
        /// <summary>
        /// Décrémente les stocks des produits en fonction de la recette commandée par le client
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="nom_recette">nom de la recette commandée</param>
        static void decrementationStock(MySqlConnection connexion, string nom_recette)
        {
            connexion.Open();

            MySqlCommand command = connexion.CreateCommand();
            command.CommandText = "SELECT nom_produit FROM contient WHERE nom_recette='" + nom_recette + "';";
            MySqlDataReader reader = command.ExecuteReader();
            List<string> produits = new List<string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string valueAsString = reader.GetValue(i).ToString();
                    produits.Add(valueAsString);
                }

            }
            reader.Close();
            foreach (string produit in produits)
            {

                command.CommandText = "update produit set utilisation=curdate(), stock=stock-(SELECT quantite_produit FROM contient WHERE nom_produit='" + produit + "' and nom_recette='" + nom_recette + "')  where nom_produit='" + produit + "';";
                command.ExecuteNonQuery();

            }
            connexion.Close();

        }
        /// <summary>
        /// Renvoie le prix d'une recette
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="nom_recette">nom de la recette</param>
        /// <returns>prix de la recette</returns>
        static int getPrix(MySqlConnection connexion, string nom_recette)
        {
            connexion.Open();
            MySqlCommand command = connexion.CreateCommand();

            command.CommandText = "select prix_vente from recette where nom_recette='" + nom_recette + "';";
            MySqlDataReader reader = command.ExecuteReader();
            int prix = 0;
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    prix += reader.GetInt32(i);
                }
            }
            reader.Close();
            connexion.Close();
            return prix;
        }
        /// <summary>
        /// soustrait du solde d'un client le prix d'une commande
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="prix">prix à payer</param>
        /// <param name="identifiant">client a prélever</param>
        static void paiement(MySqlConnection connexion, int prix, int identifiant)
        {
            connexion.Open();
            MySqlCommand command = connexion.CreateCommand();
            command.CommandText = "update client set credit_cook=credit_cook-" + prix + ";";
            command.ExecuteNonQuery();
            connexion.Close();
        }
        /// <summary>
        /// commande un plat
        /// </summary>
        /// <param name="nombre">nombre de fois qu'il faut commander le plat</param>
        /// <param name="connexion"></param>
        /// <param name="nom_recette">nom de la recette à commander</param>
        /// <returns>renvoie le prix de la commande</returns>
        static int commandePlat(int nombre, MySqlConnection connexion, string nom_recette)
        {
            int prix = 0;
            for (int i = 0; i < nombre; i++)
            {
                decrementationStock(connexion, nom_recette);
                prix += getPrix(connexion, nom_recette);
                incrementationRecette(connexion, nom_recette);
                remuneration(connexion, nom_recette);

            }
            return prix;
        }
        /// <summary>
        /// Crée la commande qui vient d'être effectuée par le client
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="identifiant">identifiant du client</param>
        /// <param name="prix">prix de la commande</param>
        static void creationCommande(MySqlConnection connexion, int identifiant, int prix)
        {
            int id_commande = CreateIdCommande(connexion);
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = " insert into commande values(" + id_commande + ",curdate()," + prix + "," + identifiant + ");";
            commande.ExecuteNonQuery();
            connexion.Close();
        }
        /// <summary>
        /// incrémente le compteur de commande d'une recette
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="nom_recette">nom de la recette à incrémenter</param>
        static void incrementationRecette(MySqlConnection connexion, string nom_recette)
        {
            connexion.Open();
            MySqlCommand command = connexion.CreateCommand();
            command.CommandText = "update recette set nombre_vente=nombre_vente+1 where nom_recette='" + nom_recette + "';";
            command.ExecuteNonQuery();
            command.CommandText = " Select nombre_vente from recette where nom_recette='" + nom_recette + "';";
            MySqlDataReader reader = command.ExecuteReader();
            int currentRowAsInt = 0;
            while (reader.Read())
            {

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    int valueAsInt = reader.GetInt32(i);
                    currentRowAsInt += valueAsInt;
                }
            }
            reader.Close();
            if (currentRowAsInt == 11)
            {
                command.CommandText = "update recette set prix_vente=prix_vente+2 where nom_recette='" + nom_recette + "';";
                command.ExecuteNonQuery();
            }
            if (currentRowAsInt == 51)
            {
                command.CommandText = "update recette set prix_vente=prix_vente+5 where nom_recette='" + nom_recette + "';";
                command.ExecuteNonQuery();
                command.CommandText = "update recette set remuneration=4 where nom_recette='" + nom_recette + "';";
                command.ExecuteNonQuery();

            }
            connexion.Close();
        }
        /// <summary>
        /// rémunère le cdr lorsque sa recette est commandée
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="nom_recette">nom de la recette commandée</param>
        static void remuneration(MySqlConnection connexion, string nom_recette)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "update client, recette set credit_cook = credit_cook + (select remuneration from recette where nom_recette='" + nom_recette + "') where client.identifiant =(select identifiant from recette where nom_recette='" + nom_recette + "');";
            commande.ExecuteNonQuery();
            connexion.Close();
        }
        /// <summary>
        /// Permet à un utilisateur de créer une recette
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="identifiant">identifiant du créateur de recette</param>
        static void creationRecette(MySqlConnection connexion, int identifiant)
        {
            Console.WriteLine("Comment s'appelle la recette?");
            string nom_recette = Console.ReadLine();
            Console.Clear();
            Console.WriteLine("Quel type de plat s'agit-il?");
            string type = Console.ReadLine();
            Console.Clear();
            Console.WriteLine("Ajoutez une description (256 caractères)");
            string description = Console.ReadLine();
            Console.Clear();
            Console.WriteLine("Quel prix coutera ce plat? (entre 10 et 40 cooks)");
            int prix_vente = 0;
            while (true)
            {
                try
                {
                    prix_vente = Convert.ToInt32(Console.ReadLine());
                    if (10 < prix_vente && prix_vente < 40)
                    {
                        break;
                    }
                }
                catch { }
            }


            Console.Clear();
            List<string> ingredients = new List<string>();
            List<int> quantite = new List<int>();


            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            Console.WriteLine("Combien d'ingrédients comporte votre recette?");
            int nombre = Convert.ToInt32(Console.ReadLine());
            for (int i = 0; i < nombre; i++)
            {
                Console.WriteLine("Nom de l'ingrédient : ");
                ingredients.Add(Console.ReadLine());
                Console.WriteLine("Quantité (en g) : ");

                while (true)
                {
                    try
                    {
                        int quantites = Convert.ToInt32(Console.ReadLine());
                        quantite.Add(quantites);
                        break;
                    }
                    catch { }
                }

            }
            for (int i = 0; i < ingredients.Count(); i++)
            {
                commande.CommandText = "insert into contient values('" + nom_recette + "','" + ingredients.ElementAt(i) + "'," + quantite.ElementAt(i) + ");";
                commande.ExecuteNonQuery();
            }

            commande.CommandText = "insert into recette VALUES ('" + nom_recette + "','" + type + "','" + description + "'," + prix_vente.ToString() + ",2," + identifiant.ToString() + " ,0,0);";
            commande.ExecuteNonQuery();

            connexion.Close();

        }
        /// <summary>
        /// Permet à un client de consulter son solde
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="identifiant">id du client qui veut consulter son solde</param>
        /// <returns>solde du client</returns>
        static int consultationCredit(MySqlConnection connexion, int identifiant)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "select credit_cook from client where identifiant='" + identifiant + "';";
            MySqlDataReader reader = commande.ExecuteReader();
            int solde = 0;
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    solde += reader.GetInt32(i);
                }
                Console.WriteLine();
            }
            reader.Close();
            connexion.Close();
            return solde;
        }
        /// <summary>
        /// Affiche toutes les recettes d'un créateur de recettes
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="identifiant"></param>
        static void affichageRecettesCdr(MySqlConnection connexion, int identifiant)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            Console.Clear();
            commande.CommandText = "select nom_recette, nombre_vente from recette where identifiant =" + identifiant + ";";
            MySqlDataReader reader = commande.ExecuteReader();
            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string recettes = reader.GetValue(i).ToString();
                    if (recettes != null)
                    {
                        Console.Write(recettes);
                    }

                }
                Console.WriteLine();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string recettes = reader.GetValue(i).ToString();
                        if (recettes != null)
                        {
                            Console.Write(recettes);
                        }

                    }
                    Console.WriteLine();
                }


            }
            else
            {
                Console.WriteLine("Vous n'avez pas créé de recette");
            }
            reader.Close();
            connexion.Close();
            Console.ReadKey();
        }
        /// <summary>
        /// affiche le cdr d'or
        /// </summary>
        /// <param name="connexion"></param>
        static void cdrDor(MySqlConnection connexion)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "select nom_client from client where identifiant= (select identifiant from recette group by identifiant order by sum(nombre_vente) desc limit 1);";
            MySqlDataReader reader = commande.ExecuteReader();

            while (reader.Read())
            {
                string currentRowAsString = "";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.WriteLine(reader.GetValue(i).ToString() + " est le cdr d'or");
                }

            }
            reader.Close();
            commande.CommandText = "select nom_recette from recette where identifiant= (select identifiant from recette group by identifiant order by sum(nombre_vente) desc limit 1) order by nombre_vente desc limit 5;";
            reader = commande.ExecuteReader();

            while (reader.Read())
            {
                string currentRowAsString = "";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string valueAsString = reader.GetValue(i).ToString();
                    currentRowAsString += valueAsString + ", ";
                }
                Console.WriteLine(currentRowAsString);
            }
            reader.Close();
            connexion.Close();
        }
        /// <summary>
        /// affiche le cdr de la semaine
        /// </summary>
        /// <param name="connexion"></param>
        static void cdrSemaine(MySqlConnection connexion)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "select nom_client from client where identifiant =(select identifiant from recette group by identifiant order by sum(nombre_vente-nombre_vente0) desc limit 1);";
            MySqlDataReader reader = commande.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine(reader.GetString(0));
            }
            reader.Close();
            connexion.Close();
        }
        /// <summary>
        /// Affiche les 5 recettes les plus commandées de la semaine
        /// </summary>
        /// <param name="connexion"></param>
        static void AffichageTop5Recettes(MySqlConnection connexion)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "select nom_recette, descriptif, prix_vente from recette order by nomBRE_vente desc limit 5;";
            MySqlDataReader reader = commande.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write(reader.GetValue(i).ToString() + "    ");
                }
                Console.WriteLine();
            }
            reader.Close();
            connexion.Close();
        }
        /// <summary>
        /// remet les compteurs de commande du debut de semaine à jour
        /// </summary>
        /// <param name="connexion"></param>
        static void RecetteDebutSemaine(MySqlConnection connexion)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "SET SQL_SAFE_UPDATES=0;update recette set nombre_vente0 = nombre_vente;SET SQL_SAFE_UPDATES = 1; ";
            commande.ExecuteNonQuery();


            connexion.Close();
        }
        /// <summary>
        /// met à jour les stocks max et mini des produits qui n'ont pas été utilisés il y a 30jours ou plus
        /// </summary>
        /// <param name="connexion"></param>
        static void MajStock(MySqlConnection connexion)
        {
            connexion.Open();
            MySqlCommand command = connexion.CreateCommand();
            command.CommandText = "SELECT nom_produit FROM produit;";
            MySqlDataReader reader = command.ExecuteReader();
            List<string> produits = new List<string>();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string valueAsString = reader.GetValue(i).ToString();
                    produits.Add(valueAsString);
                }

            }
            reader.Close();
            List<DateTime> date = new List<DateTime>();
            command.CommandText = "SELECT utilisation FROM produit;";
            reader = command.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    //DateTime valueAsString = DateTime.ParseExact(reader.GetValue(i).ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    date.Add(reader.GetDateTime(i));

                }

            }
            reader.Close();
            for (int i = 0; i < date.Count(); i++)
            {
                if ((DateTime.Today - date.ElementAt(i)).Days >= 30)
                {
                    command.CommandText = "update produit set stock_min=stock_min/2, stock_max=stock_max/2 where nom_produit='" + produits.ElementAt(i) + "';";
                    command.ExecuteNonQuery();
                }
            }

            connexion.Close();
        }
        /// <summary>
        /// Crée le fichier xml des commandes de produits
        /// </summary>
        /// <param name="connexion"></param>
        static void commandeStock(MySqlConnection connexion)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "select nom_produit from produit where stock<= stock_min;";
            MySqlDataReader reader = commande.ExecuteReader();
            List<string> produits = new List<string>();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string valueAsString = reader.GetValue(i).ToString();
                    produits.Add(valueAsString);
                }

            }
            reader.Close();
            commande.CommandText = "select nom_fournisseur from fournisseur where id_fournisseur= (select id_fournisseur from produit where stock<=stock_min);";
            reader = commande.ExecuteReader();
            List<string> fournisseur = new List<string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string valueAsString = reader.GetString(i);
                    fournisseur.Add(valueAsString);
                }

            }
            reader.Close();
            List<int> quantite = new List<int>();
            commande.CommandText = " select stock_max-stock from produit where stock<=stock_min;";
            reader = commande.ExecuteReader();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    int valueAsInt = reader.GetInt32(i);
                    quantite.Add(valueAsInt);
                }

            }
            reader.Close();
            connexion.Close();
            XmlTextWriter writer = new XmlTextWriter("product.xml", System.Text.Encoding.UTF8);
            writer.WriteStartDocument(true);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;
            writer.WriteStartElement("Fournisseur");
            for (int i = 0; i < produits.Count(); i++)
            {
                creerFournisseur(produits.ElementAt(i), quantite.ElementAt(i), fournisseur.ElementAt(i), writer);
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }
        /// <summary>
        /// crée la commande d'un produit
        /// </summary>
        /// <param name="nom_produit">nom du produit a commander</param>
        /// <param name="quantite">quantité du produit à commander</param>
        /// <param name="fournisseur">fournisseur auquel il faut commander</param>
        /// <param name="writer"></param>
        static void creerFournisseur(string nom_produit, int quantite, string fournisseur, XmlTextWriter writer)
        {
            writer.WriteStartElement(fournisseur);
            writer.WriteStartElement(nom_produit);
            writer.WriteStartElement("quantite");
            writer.WriteString(quantite.ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
        /// <summary>
        /// fonction de demo du code
        /// </summary>
        /// <param name="connexion"></param>
        static void demo(MySqlConnection connexion)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "select count(distinct identifiant) from client;";
            MySqlDataReader reader = commande.ExecuteReader();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.WriteLine("Il y a " + reader.GetValue(i).ToString() + " clients");
                }

            }
            reader.Close();
            Console.ReadKey();
            Console.Clear();
            commande.CommandText = "select count(distinct identifiant) from client where cdr <>0;";
            reader = commande.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.WriteLine("Il y a " + reader.GetValue(i).ToString() + " créateurs de recettes");
                }

            }
            reader.Close();
            Console.ReadKey();
            Console.Clear();
            commande.CommandText = "select client.nom_client ,sum(recette.nombre_vente) from client, recette where client.identifiant=recette.identifiant  group by recette.identifiant ;";
            reader = commande.ExecuteReader();
            while (reader.Read())
            {

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (i % 2 == 0)
                    {
                        Console.Write("Le créateur de recettes ");
                    }
                    if (i % 2 == 1)
                    {
                        Console.Write(" a vendu ");
                    }
                    Console.Write(reader.GetValue(i).ToString());
                    if (i % 2 == 1)
                    {
                        Console.WriteLine(" plats ");
                    }
                }

            }
            reader.Close();
            Console.ReadKey();
            Console.Clear();
            commande.CommandText = "select nom_produit from produit where stock >= 2*stock_min;";
            reader = commande.ExecuteReader();
            Console.WriteLine("le stock des produits suivants est supérieur à 2x le stock minimum : ");
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write(reader.GetValue(i).ToString() + ", ");
                }


            }

            reader.Close();
            Console.ReadKey();
            Console.Clear();
            Console.WriteLine("Quel produit souhaitez vous choisir? ");
            commande.CommandText = "select nom_recette from contient where nom_produit ='" + Console.ReadLine() + "' ;";
            reader = commande.ExecuteReader();
            Console.WriteLine("Les recettes suivantes contiennent toutes le produit que vous avez entré : ");
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write(reader.GetValue(i).ToString() + "kg, ");
                }


            }
            reader.Close();
            Console.ReadKey();
            Console.Clear();
            connexion.Close();
        }
        /// <summary>
        /// permet de se connecter à son espace client
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>si la connexion à été effectuée</returns>
        static int Login(MySqlConnection connection)
        {
            Console.Clear();
            Console.Write("Saisissez votre identifiant: ");
            int id = Convert.ToInt32(Console.ReadLine());
            Console.Write("Saisissez votre mot de passe: ");
            string mdp = Console.ReadLine();

            connection.Open();
            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT identifiant, mdp, nom_client from client";

            MySqlDataReader reader;
            reader = command.ExecuteReader();

            int id_sql = 0;
            string mdp_sql;
            bool connecte = false;

            while (reader.Read())// parcours ligne par ligne
            {
                id_sql = Convert.ToInt32(reader.GetString(0));
                mdp_sql = reader.GetString(1);
                if (id == id_sql && mdp == mdp_sql)
                {
                    Console.WriteLine("Bienvenue " + reader.GetString(2));
                    Console.WriteLine("Appuyez sur une touche pour continuer");
                    connecte = true;
                    break;
                }
            }
            connection.Close();

            if (!connecte)
            {
                Console.WriteLine("Identifiant ou mot de passe incorrect, veuillez réessayer");
                Console.WriteLine("Si vous ne possedez pas de compte, veuillez en créer un");
                Console.WriteLine("Appuyez sur une touche pour continuer");
                id_sql = 0;
            }

            Console.ReadKey();

            return id_sql;
        }
        /// <summary>
        /// crée un client
        /// </summary>
        /// <param name="connection"></param>
        static void CreerClient(MySqlConnection connection)
        {
            Console.Clear();
            Console.Write("Saisissez votre nom: ");
            String nom = Console.ReadLine();
            Console.Write("Saisissez votre mot de passe: ");
            String mdp = Console.ReadLine();
            Console.Write("Saisissez votre numéro de téléphone: ");
            int num_tel = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Souhaitez-vous créer des recettes ?" +
                              "1. Oui" +
                              "2. Non");
            int cdr = 0;
            if (Convert.ToInt32(Console.ReadLine()) == 1) { cdr = 1; }
            int id = CreateId(connection);
            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO `cooking`.`client` VALUES ('" + id + "','" + mdp + "', '" + nom + "','" + Convert.ToString(num_tel) + "', '0','" + Convert.ToString(cdr) + "');";
            command.ExecuteNonQuery();

            connection.Close();
            Console.WriteLine("Votre numéro d'identifiant est " + id);
            Console.WriteLine("Appuyez sur une touche pour continuer");

            Console.ReadKey();
        }
        /// <summary>
        /// crée un identifiant pour les nouveaux clients
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>l'identifiant du nouveau client</returns>
        static int CreateId(MySqlConnection connection)
        {
            int id = new Random().Next(10000, 100000);
            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT identifiant FROM client";

            MySqlDataReader reader;
            reader = command.ExecuteReader();

            //Cette boucle permet de vérifier si l'identifiant généré n'existe pas déja dans la base de données
            while (reader.Read())// parcours ligne par ligne
            {
                int temp_id = Convert.ToInt32(reader.GetString(0));
                if (temp_id == id) { id = new Random().Next(10000); }
            }
            connection.Close();
            return id;
        }
        /// <summary>
        /// Crée un identifiant de commande
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        static int CreateIdCommande(MySqlConnection connection)
        {
            int id = new Random().Next(10000, 100000);
            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT identifiant FROM commande";

            MySqlDataReader reader;
            reader = command.ExecuteReader();

            //Cette boucle permet de vérifier si l'identifiant généré n'existe pas déja dans la base de données
            while (reader.Read())// parcours ligne par ligne
            {
                int temp_id = Convert.ToInt32(reader.GetString(0));
                if (temp_id == id) { id = new Random().Next(10000); }
            }
            connection.Close();
            return id;
        }
        /// <summary>
        /// met a jour le statut d'un client en un cdr
        /// </summary>
        /// <param name="connexion"></param>
        /// <param name="identifiant">identifiant de la personne </param>
        static void devientCdr(MySqlConnection connexion, int identifiant)
        {
            connexion.Open();
            MySqlCommand commande = connexion.CreateCommand();
            commande.CommandText = "update client set cdr= 1 where identifiant =" + identifiant + ";";
            commande.ExecuteNonQuery();
            connexion.Close();
        }
        /// <summary>
        /// interface utilisateur
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="identifiant"></param>
        static void Interface(MySqlConnection connection, int identifiant)
        {
            Console.Clear();
            bool continuer = true;
            while (continuer)
            {
                Console.WriteLine("Choisissez une option:\n" +
                                  "1. Passer une commande\n" +
                                  "2. Créer une recette\n" +
                                  "3. Consulter ses cooks\n" +
                                  "4. Consulter ses recettes\n" +
                                  "5. Se déconnecter");
                switch (Convert.ToInt32(Console.ReadLine()))
                {
                    case 1:
                        Console.Clear();
                        affichageRecettes(connection);
                        int prix = 0;
                        bool condition = true;
                        while (condition)
                        {
                            Console.WriteLine("Entrez le nom du plat que vous souhaitez commander");
                            string nom_recette = Console.ReadLine();
                            Console.WriteLine("Quelle quantité en voulez vous?");
                            int quantite = Convert.ToInt32(Console.ReadLine());
                            prix += commandePlat(quantite, connection, nom_recette);
                            Console.WriteLine("Voulez vous commander d'autres plats?");
                            condition = Console.ReadLine() == "oui";

                        }
                        creationCommande(connection, identifiant, prix);
                        int solde = consultationCredit(connection, identifiant);
                        Console.Clear();
                        if (solde >= prix)
                        {
                            Console.WriteLine("La commande coute " + prix + " cooks et il vous reste " + solde + " vous n'avez donc pas besoin de racheter des cooks");
                            paiement(connection, prix, identifiant);
                        }
                        else
                        {
                            Console.WriteLine("Veuillez acheter " + (prix - solde) + " cooks");
                        }

                        break;
                    case 2:
                        devientCdr(connection, identifiant);
                        creationRecette(connection, identifiant);
                        Console.WriteLine("La recette a bien été créée");
                        Console.ReadKey();

                        break;
                    case 3:
                        connection.Close();
                        Console.WriteLine("il vous reste " + consultationCredit(connection, identifiant) + " cooks");

                        break;
                    case 4:
                        affichageRecettesCdr(connection, identifiant);
                        break;
                    case 5:
                        continuer = false;
                        break;
                }
            }
        }
        /// <summary>
        /// tableau de bord
        /// </summary>
        /// <param name="connection"></param>
        static void TableauBord(MySqlConnection connection)
        {
            Console.Clear();
            Console.Write("Le cdr de la semaine est : ");
            cdrSemaine(connection);
            Console.ReadKey();
            Console.Clear();
            cdrDor(connection);
            Console.ReadKey();
            Console.Clear();
            Console.WriteLine("Les 5 recettes les plus commandées cette semaines sont : ");
            AffichageTop5Recettes(connection);
            Console.ReadKey();
            Console.Clear();
        }
        /// <summary>
        /// supprime une recette
        /// </summary>
        /// <param name="connection"></param>
        static void SuppRecette(MySqlConnection connection)
        {
            Console.Clear();
            Console.Write("Saisissez le nom de la recette à supprimer: ");
            string nom_recette = Console.ReadLine();

            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM `cooking`.`recette` WHERE (`nom_recette` = '" + nom_recette + "');";
            command.ExecuteNonQuery();

            connection.Close();
        }
        /// <summary>
        /// supprime un cdr
        /// </summary>
        /// <param name="connection"></param>
        static void SuppCdr(MySqlConnection connection)
        {
            Console.Clear();
            Console.Write("Saisissez l'identifiant du créateur de recettes à supprimer: ");
            string id_cdr = Console.ReadLine();

            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE `cooking`.`client` SET `cdr` = '0' WHERE (`identifiant` = '" + id_cdr + "');" +
                                  "DELETE FROM `cooking`.`recette` WHERE (`identifiant` = '" + id_cdr + "');";
            command.ExecuteNonQuery();

            connection.Close();
        }
        /// <summary>
        /// Affiche les options principales d'un admin telles que voir les stocks, supprimer une recette...
        /// </summary>
        /// <param name=""></param>
        static void InterfaceAdmin(MySqlConnection connection)
        {
            bool continuer = true;
            while (continuer)
            {
                Console.Clear();
                Console.WriteLine("Choisissez une option: \n" +
                                  "1. Voir le tableau de bord \n" +
                                  "2. Voir les stocks d'un produit \n" +
                                  "3. Générer le fichier xml de commandes \n" +
                                  "4. Mettre à jour les stocks non utilisés \n" +
                                  "5. Mettre à jour les commandes de la semaine\n" +
                                  "6. Mettre à jour les ventes de la semaine\n" +
                                  "7. Supprimer une recette \n" +
                                  "8. Supprimer un cuisinier \n" +
                                  "9. Se déconnecter");
                switch (Convert.ToInt32(Console.ReadLine()))
                {
                    default:
                        break;
                    case 1:
                        TableauBord(connection);
                        break;
                    case 2:
                        ViewStock(connection);
                        break;
                    case 3:
                        commandeStock(connection);
                        break;
                    case 4:
                        MajStock(connection);
                        break;
                    case 5:
                        RecetteDebutSemaine(connection);
                        Console.WriteLine("Les commandes de la semaine ont bien été mis à jour.");
                        break;
                    case 6:
                        RecetteDebutSemaine(connection);
                        break;
                    case 7:
                        SuppRecette(connection);
                        break;
                    case 8:
                        SuppCdr(connection);
                        break;
                    case 9:
                        continuer = false;
                        break;
                }
                Console.WriteLine("Appuyez sur une touche pour continuer");
                Console.ReadKey();
            }
        }
        /// <summary>
        /// Permet d'afficher le stock actuel et maximal d'un produit en particulier
        /// </summary>
        /// <param name="connection"></param>
        static void ViewStock(MySqlConnection connection)
        {
            Console.Clear();
            Console.Write("Saisissez le nom du produit: ");
            String nom_produit = Console.ReadLine();

            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT nom_produit, stock, stock_max FROM produit WHERE nom_produit ='" + nom_produit + "'; ";

            MySqlDataReader reader;
            reader = command.ExecuteReader();

            while (reader.Read())// parcours ligne par ligne
            {
                Console.WriteLine("Nom du produit : " + reader.GetString(0) +
                                  "Stock actuel :" + reader.GetString(1) +
                                  "Stock max :" + reader.GetString(2));
            }
            connection.Close();

        }
    }
}
