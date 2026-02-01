using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using MARS.OpicsObjects.Extension.fileSelection;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Mars.thirdPartAppSupport.opics
{
    public class OpicsObjectConvertor
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(OpicsObjectConvertor));


        private static MarsEntities marsEntities;
        private static TransactionScope transactionScope;
        private static long targetApplicationId;
        private static int gridcellTypeId;
        public static TransactionScope onObjectGenBeginHandle(string strDBIdx,ref bool isOk, ref string strError)
        {
            try
            {                
                transactionScope = new TransactionScope(TransactionScopeOption.Required);
                gridcellTypeId = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeIDByName(strDBIdx,"MARSGRIDCELL_OPICS", ref isOk, ref strError);
                return transactionScope;
            }
            catch (Exception e)
            {
                strError = "Can't create Transaction object";
                return null;
            }            
        }

        public static long OnGetDefaultApplicationConvertorForHandle(ref bool isOk, ref string strError, string applicationNameIdx = "opics")
        {
            Logger.logBegin("OnGetDefaultApplicationConvertorForHandle");
            if (marsEntities == null)
            {
                marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            }
            T_REGISTERED_APPSDTO appDto = B_REGISTERED_APPS.GetApplicationByIdxShortName(
                MarsMainWindow.CurrentDatabaseIdx,
                applicationNameIdx, ref isOk, ref strError, marsEntities );
            if ((!isOk) || (appDto == null)) return targetApplicationId = -1;
            return targetApplicationId = appDto.APPLICATION_ID;            
        }

        public static bool ObjectGenEndHandle(object objFromBegin, bool resultLast, ref string strError)
        {
            TransactionScope trans = objFromBegin as TransactionScope;
            if (trans == null)
            {
                strError = "no transaction object passed. ";
                return false;
            }
            if (!resultLast)
            {
                //rollback
                //do nothing
                return true;
            }
            else
            {
                try
                {
                    marsEntities.SaveChanges();
                    trans.Complete();
                    MarsDBGlobe_Cache.UpdateObjectsCache();
                    return true;
                }catch(Exception e)
                {
                    Logger.Error("ObjectGenEndHandle", strError = string.Format("exception:[{0}]",e.Message));
                    
                    return false;
                }
            }
            
        }

        public static bool GeneratePegwinObjHandle(object objFromBegin, string strObjectName, string strObjectInfo, ref object pegwinStub, ref string strError)
        {
            Logger.logBegin("GeneratePegwinObjHandle");
            if (marsEntities == null)
            {
                marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);                
            }
            try
            {
                B_REGISTED_OBJECT tmpPeg = new B_REGISTED_OBJECT() {
                    APPLICATION_ID = targetApplicationId,
                    OBJECT_HAPPY_NAME = strObjectName,
                    QUICK_ACCESS = strObjectInfo
                };
                bool isOk = B_REGISTED_OBJECT.InsertObjectInTrans(MarsMainWindow.CurrentDatabaseIdx, tmpPeg, ref strError, true, marsEntities);
                if (!isOk) return false;
                pegwinStub = tmpPeg;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GeneratePegwinObjHandle", strError = string.Format("Exception:[{0}]", e.Message));
                return false;
            }
            finally
            {
                Logger.logEnd("GeneratePegwinObjHandle");
            }
        }

        public static bool GenerateObjectHandle(object objFromBegin, object pegwinStub,
            string strObjectName,
            string strObjectInfo,
            ref OPICS_OBJECT_CONVERT_ERROR_CODE errorCode,
            ref string strError)
        {
            Logger.logBegin("GenerateObjectHandle", string.Format("objName=[{0}], objInfo=[{1}]", strObjectName, strObjectInfo));
            try
            {
                
                if ((pegwinStub == null)||(!(pegwinStub is B_REGISTED_OBJECT)))
                {
                    Logger.Error("GenerateObjectHandle", strError = "pegwindow object is null") ;
                    errorCode = OPICS_OBJECT_CONVERT_ERROR_CODE.NO_PEG;
                    return false;
                }

                B_REGISTED_OBJECT tmpPeg = pegwinStub as B_REGISTED_OBJECT;
                B_REGISTED_OBJECT tmpObj = new B_REGISTED_OBJECT()
                {
                    APPLICATION_ID = targetApplicationId,
                    OBJECT_HAPPY_NAME = strObjectName,
                    OBJECT_TYPE = tmpPeg.OBJECT_HAPPY_NAME,
                    QUICK_ACCESS = strObjectInfo,
                    TYPE_ID = gridcellTypeId
                };
                bool isOk = B_REGISTED_OBJECT.InsertObjectInTrans(MarsMainWindow.CurrentDatabaseIdx, tmpObj, ref strError, false, marsEntities);
                if (!isOk)
                {
                    errorCode = OPICS_OBJECT_CONVERT_ERROR_CODE.CANT_INSERT_OBJECT;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                errorCode = OPICS_OBJECT_CONVERT_ERROR_CODE.OTHER_ERROR;
                Logger.Error("GenerateObjectHandle",strError = string.Format("exception:[{0}]", e.Message));
                return false;
            }
            finally
            {
                Logger.logEnd("GenerateObjectHandle");
            }
            
        }

        internal static bool OnAfterTransactionIsDoneHandle(ref string strError)
        {
             return B_REGISTED_OBJECT.UpdateMaterializedViews(MarsMainWindow.CurrentDatabaseIdx, null, ref strError);
        }
    }
}
