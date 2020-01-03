﻿using Realms;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Soulful.Core.Model.CardDb
{
    public static class RealmHelpers
    {
        public static Realm GetRealmInstance()
        {
            string realmPath = App.GetAppdataFilePath("Soulful.realm");
            RealmConfiguration config = new RealmConfiguration(realmPath)
            {
                ObjectClasses = new Type[] { typeof(Preferences) }
            };
            return Realm.GetInstance(config);
        }

        public static Realm GetCardsRealm()
        {
            string assemPath = Assembly.GetEntryAssembly().Location;
            assemPath = Path.GetDirectoryName(assemPath);
            string realmPath = Path.Combine(assemPath, "Resources", "cards.realm");
            RealmConfiguration config = new RealmConfiguration(realmPath)
            {
                ObjectClasses = new Type[] { typeof(Pack), typeof(WhiteCard), typeof(BlackCard) }
            };
            return Realm.GetInstance(config);
        }

        public static Preferences GetUserPreferences(Realm instance = null)
        {
            if (instance == null)
                instance = GetRealmInstance();
            IQueryable<Preferences> preferences = instance.All<Preferences>();

            if (preferences.Count() == 0)
            {
                Preferences p = new Preferences();
                instance.Write(() => instance.Add(p));
                return p;
            }
            else
            {
                return preferences.First();
            }
        }
    }
}
