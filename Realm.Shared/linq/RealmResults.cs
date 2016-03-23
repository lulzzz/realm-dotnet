﻿/* Copyright 2015 Realm Inc - All Rights Reserved
 * Proprietary and Confidential
 */
 
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Realms
{
    /// <summary>
    /// Iterable collection of one kind of RealmObject resulting from Realm.All or from a LINQ query expression.
    /// </summary>
    /// <typeparam name="T">Type of the RealmObject which is being returned.</typeparam>
    public class RealmResults<T> : IQueryable<T>
    {
        public Type ElementType => typeof (T);
        public Expression Expression { get; } = null; // null if _allRecords
        private readonly RealmResultsProvider _provider = null;  // null if _allRecords
        private readonly bool _allRecords = false;
        private readonly Realm _realm;

        internal ResultsHandle ResultsHandle => _resultsHandle ?? (_resultsHandle = CreateResultsHandle()); 
        private ResultsHandle _resultsHandle = null;

        public IQueryProvider Provider => _provider;

        internal RealmResults(Realm realm, RealmResultsProvider realmResultsProvider, Expression expression,
            bool createdByAll)
        {
            _realm = realm;
            _provider = realmResultsProvider;
            Expression = expression ?? Expression.Constant(this);
            _allRecords = createdByAll;

        }

        internal RealmResults(Realm realm, bool createdByAll)
            : this(realm, new RealmResultsProvider(realm), null, createdByAll)
        {
        }

        private ResultsHandle CreateResultsHandle()
        {
            var retType = typeof (T);
            if (_allRecords)
            {
                return _realm.MakeResultsForTable(retType);
            }
            else
            {
                // do all the LINQ expression evaluation to build a query
                var qv = _provider.MakeVisitor(retType);
                qv.Visit(Expression);
                var queryHandle = qv._coreQueryHandle; // grab out the built query definition
                return _realm.MakeResultsForQuery(retType, queryHandle);
            }
        }

        /// <summary>
        /// Standard method from interface IEnumerable allows the RealmResults to be used in a <c>foreach</c>.
        /// </summary>
        /// <returns>An IEnumerator which will iterate through found Realm persistent objects.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new RealmResultsEnumerator<T>(_realm, ResultsHandle);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();  // using our class generic type, just redirect the legacy get
        }


        /// <summary>
        /// Count all objects if created by <see cref="Realm.All"/> of the parameterised type, faster than a search.
        /// </summary>
        /// <remarks>
        /// Resolves to this method instead of the static extension <c>Count&lt;T&gt;(this IEnumerable&lt;T&gt;)</c>.
        /// </remarks>
        public int Count()
        {
            if (_allRecords)
            {
                // use the type captured at build based on generic T
                var tableHandle = _realm._tableHandles[ElementType];
                return (int)NativeTable.count_all(tableHandle);
            }
            // we should be in RealmQRealmResultsr.VisitMethodCall, not here, ever, seriously!
            throw new NotImplementedException("Count should not be invoked directly on a RealmResults created by All. LINQ will not invoke this."); 
        }    

    }  // RealmResults
}