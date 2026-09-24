using Glove.Model;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Glove.Properties;

namespace Glove
{
    internal class ProductList
    {
        internal static int UpdatePromotions(string shop, int status_i)
        {
            using(SqlConnection con = new SqlConnection(Resources.connStr))
            {
                con.Open();
           
                var sqlpromoUpdate = @"update Glovo_Master_Podaci set promotion_id= 
                                (select cast(@shop as bigint) + CAST(abs(CAST(CRYPT_GEN_RANDOM(8) as bigint)) 
                                % CAST(1E16 AS bigint) AS CHAR(16))) 
                                where sif_obj = @shop and status_i = @status_i
                                and redovna_cena is not null 
                                and mp_cena < redovna_cena
                                and dostupan = 1
                                and neaktivnost = 0
                                ";

                try
                {
                    SqlCommand cmd = new SqlCommand(sqlpromoUpdate, con);
                    cmd.Parameters.Add(new SqlParameter("@shop", shop));
                    cmd.Parameters.Add(new SqlParameter("@status_i", status_i));

                    return cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    return 0;
                }
            }
        }

        internal static object GetProductsAndPromotions(string shop, int status_i)
        {
            using (SqlConnection con = new SqlConnection(Resources.connStr))
            {
                con.Open();
                var sql = @"begin transaction
                            update Glovo_Master_Podaci set status_i='8' where id in(
                            select top 5000 id  
                            from Glovo_Master_Podaci where sif_obj = @shop and status_i = @status_i and redovna_cena is not null
                            )
                            select sifra_artikla, naziv_artikla, opis_akcija, redovna_cena, dostupan  
                            from Glovo_Master_Podaci where sif_obj = @shop and status_i = '8'
                        commit transaction";


                try
                {

                    // Get Products
                    SqlCommand cmdproducts = new SqlCommand(sql, con);
                    cmdproducts.Parameters.Add(new SqlParameter("@shop", shop));
                    cmdproducts.Parameters.Add(new SqlParameter("@status_i", status_i)); // ovde status_i samo u update delu, select-u ga ne trebam.

                    SqlDataReader readerproducts = cmdproducts.ExecuteReader();
                    List<Product> ls = new();
                    while (readerproducts.Read())
                    {
                        ls.Add(new Product()
                        {
                            id = (string)readerproducts["sifra_artikla"],
                            name = (string)readerproducts["naziv_artikla"],
                            description = (readerproducts["opis_akcija"] == DBNull.Value) ? string.Empty : (string)readerproducts["opis_akcija"],
                            price = (decimal)readerproducts["redovna_cena"],
                            available = (bool)readerproducts["dostupan"]
                        });
                    }

                    readerproducts.Close();

                    List<Attributes> al = new();

                    Root root = new();


                    root.products = ls;
                    root.attributes = al;

                    return root;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }
        internal static object GetShopID() // Production
        {
            using (SqlConnection con = new SqlConnection(Resources.connStr))
            {
                con.Open();
                var sql = "select distinct sif_obj from Glovo_Master_Podaci where status_i = '1'";
                try
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    SqlDataReader reader = cmd.ExecuteReader();
                    List<ShopId> shopid = new();

                    while (reader.Read())
                    {
                        shopid.Add(new ShopId()
                        {
                            shopid = (string)reader["sif_obj"].ToString()
                        });
                    }
                    reader.Close();
                    return shopid;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }

        internal static int GetProductsCount(string shop, int status_i)
        {
            using (SqlConnection con = new SqlConnection(Resources.connStr))
            {
                con.Open();
                var sql = @"select count(*) 
                        from Glovo_Master_Podaci where sif_obj = @shop and status_i = @status_i";

                try
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.Add(new SqlParameter("@shop", shop));
                    cmd.Parameters.Add(new SqlParameter("@status_i", status_i));

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    return 0;
                }
            }
        }

        internal static object UpdGlovo_EvidencijaServisa()
        {
            using (SqlConnection con = new SqlConnection(Resources.connStr))
            {
                con.Open();
                var sql = "Update Glovo_EvidencijaServisa set vreme_azuriranja = getdate()";
                try
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }

        }

        internal static int InsGlovo_Log(string shop, string status, string response)
        {
            using (SqlConnection con = new SqlConnection(Resources.connStr))
            {
                con.Open();
                int id_log = 0;
                var sql = "INSERT into Glovo_Log (sif_obj, response_status, json, d_ins) " +
                          "VALUES (@sif_obj, @response, @status, getdate())" +
                          "SELECT SCOPE_IDENTITY() as id_log";
                try
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@sif_obj", shop);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@response", response);
                    id_log = Convert.ToInt32(cmd.ExecuteScalar());
                    return id_log;
                }
                catch (Exception e)
                {
                    return id_log;
                }
            }
        }

        internal static object InsGlovoTransakcije(string sif_obj, string transaction_id, string status, DateTime last_updated_at)
        {
            using (SqlConnection con = new SqlConnection(Resources.connStr))
            {
                con.Open();
                var sql = "INSERT into Glovo_Transakcije (sif_obj, transaction_id, status, last_updated_at, d_ins, d_upd) " +
                "VALUES (@sif_obj, @transaction_id, @status, dateadd(hour,2,@last_updated_at), getdate(), getdate())" +
                "Update Glovo_Master_Podaci set status_i = '2', Glovo_Transakcija_Id = @transaction_id where sif_obj = @sif_obj and status_i = '8'"; // ovo ispraviti / preprvio status_i = 8
                try
                {
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@sif_obj", sif_obj);
                    cmd.Parameters.AddWithValue("@transaction_id", transaction_id);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@last_updated_at", last_updated_at);
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }
    }
}
