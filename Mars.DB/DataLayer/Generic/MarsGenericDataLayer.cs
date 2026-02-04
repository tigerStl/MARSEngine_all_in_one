using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using System.Linq.Expressions;

namespace Mars.message.DataLayer.Generic
{
    #region Tiger Data Transcation Interface
    public enum EN_TRANSCATION_DEALSTAUTS
    {
        EN_ERROR = -0x1,
        EN_IGNORE,
        EN_OK
    }
    public interface IMarsTigerTranscation
    {
        Type GetBOEntityType();
        bool ModifyObject(MarsEntities objEntityMgr, ref string strError);
        EN_TRANSCATION_DEALSTAUTS RemoveByTranscation(MarsEntities transcationEntityMgr, ref string strError);
        EN_TRANSCATION_DEALSTAUTS AddByTranscation(string strDBIdx, MarsEntities transcationEntityMgr, ref string strError);
    }
    #endregion //Tiger Data Transcation

    #region Tiger Data generic Data Layer

    public delegate int Entities_UpdateContent<T>(T instObj);

    public interface IMarsDataAccessLayer<T> where T : class
    {
        IList<T> GetAll(params Expression<Func<T, object>>[] navigationProperties);
        IList<T> GetList(Func<T, bool> where, params Expression<Func<T, object>>[] navigationProperties);
        T GetSingle(Func<T, bool> where, params Expression<Func<T, object>>[] navigationProperties);
        int AddSingle(T objInst);

    }

    public class MarsDataAccessLayer<T> : IMarsDataAccessLayer<T> where T : class
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsDataAccessLayer<T>));

        private string currrentDBIdx = MarsEntitiesExtends.cnst_default_dbName;
        public MarsDataAccessLayer(string strDBIdx)
        {
            currrentDBIdx = strDBIdx;
        }

        public virtual IList<T> GetAll(params Expression<Func<T, object>>[] navigationProperties)
        {
            Logger.logBegin("GetAll");

            List<T> lst;

            MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: currrentDBIdx);
            IQueryable<T> objQuery = objEntities.Set<T>();

            foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
            {
                objQuery = objQuery.Include<T, object>(navigationProperty);
            }
            lst = objQuery.AsNoTracking().ToList<T>();
            Logger.logEnd("GetAll");
            return lst;
        }

        public virtual int AddSingle(T objInst)
        {
            Logger.Info("UpdateSingle", objInst == null ? "NULL" : objInst.ToString());
            MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: currrentDBIdx);
            DbSet<T> objD = objEntities.Set<T>();
            objD.Add(objInst);
            objEntities.SaveChanges();
            return 1;
        }

        public Entities_UpdateContent<T> updateCurrentSingle = null;
        public virtual int UpdateSingle(Func<T, bool> where, ref string strError)
        {
            Logger.logBegin("UpdateSingle", string.Format("Type:[{0}]", typeof(T)));
            if (updateCurrentSingle == null)
            {
                Logger.Error("UpdateSingle", strError = "No Delegate for update Single is assigned. Assign it first");
                return -1;
            }
            try
            {
                MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: currrentDBIdx);
                DbSet<T> objQuery = objEntities.Set<T>();
                T objInst = objQuery.Where<T>(where).Single();
                if (objInst == null)
                {
                    Logger.Error("UpdateSingle", strError = string.Format("Can't get single object for where:[{0}]", objQuery.ToString()));
                    return -1;
                }

                objQuery.Attach(objInst);
                int iRslt = updateCurrentSingle(objInst);

                if (iRslt > 0)
                {
                    objEntities.SaveChanges();
                    Logger.Info("UpdateSingle", string.Format("[{0}] objects are updated", iRslt));
                }
                return 1;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateSingle", strError = string.Format("Exceptions:[{0}]", e.Message), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("UpdateSingle");
            }
        }


        public virtual IList<T> GetList(Func<T, bool> where, 
            params Expression<Func<T, object>>[] navigationProperties)
        {
            Logger.logBegin("GetList");
            List<T> lst;
            try
            {
                MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: currrentDBIdx);
                IQueryable<T> objQuery = objEntities.Set<T>();
                if (navigationProperties != null)
                    foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
                        objQuery = objQuery.Include<T, object>(navigationProperty);

                //var objNoTrack = objQuery.AsNoTracking();
                //var objW = objNoTrack.Where(where);
                var objW = objQuery.Where(where);
                //Logger.Info("GetList", "objQuery:"+objQuery.ToString());
                if (objW == null)
                    return new List<T>();
                lst = objW.ToList();
                //lst = objQuery.AsNoTracking().Where<T>(where).ToList<T>();
                return lst;
            }
            catch (Exception e)
            {
                Logger.Error("GetList", string.Format("Exceptions:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetList");
            }


        }
        public virtual T GetSingle(Func<T, bool> where, params Expression<Func<T, object>>[] navigationProperties)
        {
            Logger.logBegin("GetSingle");
            //List<T> lst;
            MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: currrentDBIdx);
            IQueryable<T> objQuery = objEntities.Set<T>();
            foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
                objQuery = objQuery.Include<T, object>(navigationProperty);
            Logger.logEnd("GetSingle");
            return objQuery.AsNoTracking().Where(where).FirstOrDefault<T>();
        }


        /*
        public static string ToTraceString<T>(IQueryable<T> query)
        {
            var internalQueryField = query.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(f => f.Name.Equals("_internalQuery")).FirstOrDefault();

            var internalQuery = internalQueryField.GetValue(query);

            var objectQueryField = internalQuery.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(f => f.Name.Equals("_objectQuery")).FirstOrDefault();

            var objectQuery = objectQueryField.GetValue(internalQuery) as System.Data.Entity.Core.Objects.ObjectQuery<T>;

            return ToTraceStringWithParameters<T>(objectQuery);
        }
        public static string ToTraceStringWithParameters<T>(System.Data.Entity.Core.Objects.ObjectQuery<T> query)
        {
            System.Text.StringBuilder sb = new StringBuilder();

            string traceString = query.ToTraceString() + Environment.NewLine;

            foreach (var parameter in query.Parameters)
            {
                traceString += parameter.Name + " [" + parameter.ParameterType.FullName + "] = " + parameter.Value + "\n";
            }

            return traceString;
        }
        */
    }
    #endregion //Tiger Data generic Data Layer
}
