﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FitBooking.Models;
using DHTMLX.Scheduler;
using DHTMLX.Scheduler.Data;
using DHTMLX.Common;
using DHTMLX.Scheduler.Controls;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.AspNet.Identity.EntityFramework;

namespace FitBooking.Controllers
{
    [RoutePrefix("Kalendarz")]
    public class SpotkaniesController : Controller
    {
        private Entities3 db = new Entities3();
        public static int? idl;


        Uzytkownik getUser()
        {
            if (Request.IsAuthenticated == true)
            {
                var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
                Uzytkownik p = db.Uzytkownik.SingleOrDefault(k => k.id_aspUser == u.Id);
                return p;
            }
            else return null;

        }

        Uzytkownik getUserID(int? id)
        {

            //var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
            Uzytkownik p = db.Uzytkownik.SingleOrDefault(k => k.Id == id);
            return p;

        }
        string rolaUser()
        {
            if (Request.IsAuthenticated == true)
            {
                ApplicationDbContext db1 = new ApplicationDbContext();
                var listOfUsers = (from u in db1.Users
                                   let query = (from ur in db1.Set<IdentityUserRole>()
                                                where ur.UserId.Equals(u.Id)
                                                join r in db1.Roles on ur.RoleId equals r.Id
                                                select r.Name)
                                   select new UserRoleInfo() { User = u, Roles = query.ToList<string>() })
                                 .ToList();
                foreach (UserRoleInfo user in listOfUsers)
                {
                    if (getUser().id_aspUser == user.User.Id)
                        return user.Roles.FirstOrDefault();

                }
                return null;
            }
            else
                return null;
        }

        public string changeColor(string status)
        {
            if (status == "dostepne") return "#baed91";
            else if (status == "zarezerwowane") return "#fea3aa";
            else return "#8CD1E6";

        }


        [Route("")]
        // GET: Spotkanies
        public ActionResult Index(int? id)
        {
            ModelCaledar kalendarz = new ModelCaledar();

            var scheduler = new DHXScheduler(this);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.ActiveLinks);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Limit);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Collision);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Readonly);

            scheduler.Extensions.Add("../scheduler.config.js");
            scheduler.Extensions.Add("../scheduler-client.js");
            // scheduler.AfterInit.Add("readonlyEvents();");
            scheduler.BeforeInit.Add("init();");
            scheduler.BeforeInit.Add("readonlyEvents();");
            // scheduler.Extensions.Add("../scheduler-client.js");
            scheduler.BeforeInit.Add("scheduler.customConfiguration();");
            scheduler.Skin = DHXScheduler.Skins.Flat;
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            scheduler.EnableDynamicLoading(SchedulerDataLoader.DynamicalLoadingMode.Month);
            scheduler.Localization.Set(SchedulerLocalization.Localizations.Polish);
            scheduler.LoadData = true;
            scheduler.EnableDataprocessor = true;
            scheduler.UpdateFieldsAfterSave();
            scheduler.Config.isReadonly = true;


            // scheduler.AfterInit.Add("block_readonly();");
            //var idl = id;
            int? idZalogowanego = null;
            if (rolaUser() != null) idZalogowanego = getUser().Id;

            if (rolaUser() == "klient" && id != null && id != idZalogowanego) // dla klienta i nie zalogowanego uzytkownika
            {
                kalendarz.klient = getUser();
                kalendarz.funkcyjna = getUserID(id);
            }
            if (rolaUser() == "trener" || rolaUser() == "dietetyk")
            {

                if (idZalogowanego == id || id == null)
                {

                    scheduler.Lightbox.Add(new LightboxText("text", "Opis") { Height = 42, Focus = true });
                    var select = new LightboxSelect("status", "status");
                    var items = new List<object>(){
                         new { key = "dostepne", label = "dostepne" },
                         new { key = "zarezerwowane", label = "zarezerwowane"},
                         new { key = "inne", label = "inne" }
                        };
                    select.AddOptions(items);
                    scheduler.Lightbox.Add(select);
                    scheduler.Lightbox.Add(new LightboxTime("time", "Data"));
                    scheduler.Config.isReadonly = false;
                    if (id == null)
                    {
                        kalendarz.funkcyjna = getUser();
                        List<Spotkanie> pom = new List<Spotkanie>();
                        var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == kalendarz.funkcyjna.Id).ToList();
                        foreach (Lista_spotkan sp in spotkania)
                        {
                            pom.Add(sp.Spotkanie);

                        }
                        kalendarz.lista = pom;


                    }
                    else
                    {
                        kalendarz.funkcyjna = getUserID(id);
                        var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == kalendarz.funkcyjna.Id).ToList();

                        foreach (Lista_spotkan sp in spotkania)
                        {
                            kalendarz.lista.Add(sp.Spotkanie);

                        }


                    }

                }
            }
            else // czyli inny trener wchodzi na konto innego trenerea to co klient 
            {

                scheduler.Config.readonly_form = true;
            }


            idl = id;
            kalendarz.scheduler = scheduler;

            var stands =
       kalendarz.lista
         .Where(s => s.color == "#baed91" && s.data_start > DateTime.Now)
         .Select(s => new
         {
             Id = s.Id,
             Description = string.Format("{0}-{1}", s.data_start.Value.ToString("MM/dd/yyyy HH:mm"), s.data_koniec.Value.TimeOfDay.ToString(@"hh\:mm"))
         })
         .ToList();

            ViewBag.spotkanieID = new SelectList(stands, "Id", "Description");


            return View(kalendarz);
        }





        // [Authorize(Roles = "dietetyk,tener")]
        public ContentResult Data()
        {

            var id = idl;
            dynamic s;

            var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
            Uzytkownik p = db.Uzytkownik.SingleOrDefault(k => k.id_aspUser == u.Id);
            List<Spotkanie> apps = new List<Spotkanie>();
            List<dynamic> lista = new List<dynamic>(); ;
            int? idZalogowanego = getUser().Id;




            if (rolaUser() == "klient" && id == null) // kalendarz klienta 
            {
                id = idZalogowanego;
                var spotkania = db.Lista_spotkan.Where(x => x.id_klient == p.Id).ToList();
                foreach (Lista_spotkan sp in spotkania)
                {
                    s = new { id = sp.Spotkanie.Id, start_date = sp.Spotkanie.data_start, end_date = sp.Spotkanie.data_koniec, text = sp.Spotkanie.opis, color = sp.Spotkanie.color, @readonly = false };
                    lista.Add(s);
                }

            }
            else
            {
                if (id == null || id == idZalogowanego) // dla trenerow i dietyetykow aby mogli edytowac
                {
                    id = idZalogowanego;
                    var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == id).ToList();
                    foreach (Lista_spotkan sp in spotkania)
                    {
                        s = new { id = sp.Spotkanie.Id, start_date = sp.Spotkanie.data_start, end_date = sp.Spotkanie.data_koniec, text = sp.Spotkanie.opis, color = sp.Spotkanie.color, @readonly = false };
                        lista.Add(s);

                    }
                }
                else
                { //id!=od idza
                    var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == id).ToList();

                    foreach (Lista_spotkan sp in spotkania)
                    {
                        s = new { id = sp.Spotkanie.Id, start_date = sp.Spotkanie.data_start, end_date = sp.Spotkanie.data_koniec, text = sp.Spotkanie.opis, color = sp.Spotkanie.color, @readonly = true };
                        lista.Add(s);

                    }
                }
            }
            var data = new SchedulerAjaxData(lista);
            return data;
        }



        public ContentResult Save(int? id, FormCollection actionValues)
        {
            var action = new DataAction(actionValues);

            // actionVa
            try
            {

                var changedEvent = (Spotkanie)DHXEventsHelper.Bind(typeof(Spotkanie), actionValues);

                switch (action.Type)
                {
                    case DataActionTypes.Insert:
                        var status = actionValues["status"].ToString();
                        changedEvent.color = changeColor(status);
                        db.Spotkanie.Add(changedEvent);
                        db.SaveChanges();
                        int idlast = db.Spotkanie.Max(s => s.Id);
                        Lista_spotkan l = new Lista_spotkan();
                        l.id_spotkanie = idlast;
                        var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
                        Uzytkownik p = db.Uzytkownik.SingleOrDefault(k => k.id_aspUser == u.Id);
                        l.id_funkcyjna = p.Id;
                        l.status = status;
                        db.Lista_spotkan.Add(l);
                        db.SaveChanges();

                        break;
                    case DataActionTypes.Delete:

                        var instance = db.Spotkanie.FirstOrDefault(o => o.Id == id);
                        var deletedL = db.Lista_spotkan.FirstOrDefault(m => m.id_spotkanie == instance.Id);

                        if (instance != null)
                        {
                            db.Entry(deletedL).State = EntityState.Deleted;
                            db.SaveChanges();
                            db.Entry(changedEvent).State = EntityState.Deleted;
                            db.SaveChanges();
                        }
                        else action.Type = DataActionTypes.Error;
                        break;

                    default:
                        var statusE = actionValues["status"].ToString();
                        changedEvent.color = changeColor(statusE);
                        db.Entry(changedEvent).State = EntityState.Modified;
                        db.SaveChanges();

                        var editL = db.Lista_spotkan.FirstOrDefault(m => m.id_spotkanie == id);
                        if (id != getUser().Id) editL.id_klient = getUser().Id;
                        editL.status = statusE;
                        db.Entry(editL).State = EntityState.Modified;
                        db.SaveChanges();

                        break;
                }
                //data.SubmitChanges();
                action.TargetId = changedEvent.Id;
            }
            catch
            {
                action.Type = DataActionTypes.Error;
            }



            return (new AjaxSaveResponse(action));
        }


 
        [HttpPost]
        public ActionResult wyslij(FormCollection collection)
        {
            
            var c = collection[0];
            var a = collection[1]; // wiadomosc
            var b = collection[2]; //mejl klient
            var s = collection[3]; //mej; funl


            return View();


        }
    }
}


