﻿// System Class
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
// Custom Class
using LothiumDB;
using LothiumDB.Providers;
using LothiumDB.Tester.TestModels;

Console.WriteLine("Start Testing Console Project");

Database<MSSqlServerProvider> db = new Database<MSSqlServerProvider>("192.168.1.124", "SA", "SntnAndr28021998", "LothiumDB_Dev", "Italian", false, false);
SqlBuilder? sql = null;

int testSection = 6;
if (testSection == 0)
{
    sql = new SqlBuilder();
    string var1 = "Property1", var2 = "Property Di Test 1";
    sql.Select("*").From(new SqlBuilder().Select("*").From("TabellaDiTest").Where("[Nome] = @0", var1).Where("[Descrizione] = @1", var2));
    var res1 = db.Query<TabellaDiTest>(sql).ToDataSet();

    // Esegue il comando Scalar e restituisce un risultato come stringa
    sql = new SqlBuilder();
    sql.Select("Nome").From("TabellaDiTest");
    string res2 = db.Scalar<string>(sql);

    // Esegue il comando Query restituendo i dati formattati secondo l'oggetto passato
    sql = new SqlBuilder();
    sql.Select("*").From("TabellaDiTest");
    List<TabellaDiTest> res3 = db.Query<TabellaDiTest>(sql);

    // Esegue il comando Query restituendo i dati in un dataset
    sql = new SqlBuilder();
    sql.Select("*").From("TabellaDiTest");
    DataSet res4 = db.Query<TabellaDiTest>(sql).ToDataSet();
}
if (testSection == 1)
{
    // Esegue un insert di un record all'interno di una tabella del database
    sql = new SqlBuilder();
    sql.Insert("TabellaDiTest", "Nome", "Descrizione").Values("Nome6", "Descrizione6");
    int rows = db.Execute(sql);

    // Esegue un update di un record all'interno di una tabella del database
    sql = new SqlBuilder();
    sql.Update("TabellaDiTest", new List<string>() { "Nome", "Descrizione" }, "NomeUPD6", "DescrizioneUPD6").Where("[Nome] = @0", "Nome6").Where("[Descrizione] = @1", "Descrizione6");
    rows = db.Execute(sql);

    // Esegue un delete di un record all'interno di una tabella del database
    sql = new SqlBuilder();
    sql.Delete("TabellaDiTest").Where("[Nome] = @0", "NomeUPD6").Where("[Descrizione] = @1", "DescrizioneUPD6");
    rows = db.Execute(sql);
}
if (testSection == 2)
{
    // Tests for the FetchAll() Methods

    var res5 = db.FetchAll<TabellaDiTest>(new SqlBuilder());
    res5 = db.FetchAll<TabellaDiTest>();
    res5 = db.FetchAll<TabellaDiTest>(0, 3);
    res5 = db.FetchAll<TabellaDiTest>("SELECT * FROM TabellaDiTest", 3, 2);
    res5 = db.FetchAll<TabellaDiTest>(new SqlBuilder().Select("*").From("TabellaDiTest"), 3, 2);

    sql = new SqlBuilder();
    sql.Where("[Nome] = @0", "Nome5").Where("[Descrizione] = @1", "Descrizione5");
    res5 = db.FetchAll<TabellaDiTest>(sql, 0, 3);
}
if (testSection == 3)
{
    var res6 = db.SingleFetch<TabellaDiTest>("Where [Nome] = @0", "Nome4");

    sql = new SqlBuilder();
    sql.Select("TOP 1 *").From("TabellaDiTest").Where("[Nome] = @0", "Nome4");
    res6 = db.SingleFetch<TabellaDiTest>(sql);
}
if (testSection == 4)
{
    var lstTbTest = db.FetchAll<TabellaDiTest>();

    TabellaDiTest tbTst1 = new TabellaDiTest()
    {
        PropertyNome = "Property 7",
        PropertyDescrizione = "Proprietà Di Test 7"
    };
    db.Insert(tbTst1);
    lstTbTest = db.FetchAll<TabellaDiTest>();

    tbTst1.PropertyDescrizione = "Property Di Test 7 (Campo Aggiornato Da Codice)";
    db.Update(tbTst1);
    lstTbTest = db.FetchAll<TabellaDiTest>();

    db.Delete(tbTst1);
    lstTbTest = db.FetchAll<TabellaDiTest>();
}
if (testSection == 5)
{
    using (sql = new SqlBuilder("SELECT * FROM TabellaDiTest Where Nome = @0", "Property 1"))
    {
        var prop = db.SingleFetch<TabellaDiTest>(sql);
    }

    using (sql = new SqlBuilder())
    {
        sql.Select("*").From("TabellaDiTest").Where("Nome = @0", "Property 1");
        var prop = db.SingleFetch<TabellaDiTest>(sql);
    }
}
if (testSection == 6)
{
    db.EnableAuditMode();

    var lst1 = db.Query<TabellaDiTest>(new SqlBuilder("SELECT * FROM TabellaDiTest")).ToDataSet();

    db.DisableAuditMode();
}

return;