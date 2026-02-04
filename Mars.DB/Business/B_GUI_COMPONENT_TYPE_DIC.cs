using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Linq;
using System.Text;

namespace Mars.message.Business
{
    public class B_GUI_COMPONENT_TYPE_DIC : T_GUI_COMPONENT_TYPE_DICDTO
    {
        private static List<T_GUI_COMPONENT_TYPE_DICDTO> CachedGuiTypeInfo = null;
        public const string CNST_PEGWINDOW_TYPE_NAME = "Pegwindow";
        public string GetControlTypeNames(string strDBIdx, long typeId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            StringBuilder sb = new StringBuilder();

            var typeNames = (from c in marsEntities.T_GUI_COMPONENT_TYPE_DIC
                             where c.TYPE_ID == typeId
                             orderby c.TYPE_NAME
                             select c);

            foreach (T_GUI_COMPONENT_TYPE_DIC typeName in typeNames)
            {
                sb.Append(typeName.TYPE_NAME);
                //sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public Dictionary<string, string> GetControlTypeList(string strDBIdx)
        {
            Dictionary<string, string> dicControlTypeList = new Dictionary<string, string>();
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            var typeNames = (from c in marsEntities.T_GUI_COMPONENT_TYPE_DIC
                             orderby c.TYPE_NAME
                             select c);

            foreach (T_GUI_COMPONENT_TYPE_DIC typeName in typeNames)
            {
                dicControlTypeList.Add(typeName.TYPE_NAME, typeName.TYPE_ID.ToString());
            }
            return dicControlTypeList;
        }


        public long GetKeywordRelationId(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif
            long projectId = (long)marsEntities.GETNEXT_VAL("T_DIC_RELATION_KEYWORD_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public List<B_GUI_COMPONENT_TYPE_DIC> GetTypeList(string strDBIdx)
        {
            List<B_GUI_COMPONENT_TYPE_DIC> lstGuiType = new List<B_GUI_COMPONENT_TYPE_DIC>();
            lstGuiType = GetObjectTypeList(strDBIdx).ToList();
            return lstGuiType;
        }

        private static MLogger Logger = MLogger.GetLogger("B_GUI_COMPONENT_TYPE_DIC");
        private static ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> typeDicList = null;
        public ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> GetObjectTypeList(string strDBIdx)
        {
            Logger.logBegin("GetObjectTypeList");
            if (typeDicList == null)
            {
                LoadTypeDicFromDB(strDBIdx);
            }
            return typeDicList;
        }

        public static ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> GetObjectTypeListEx(string strDBIdx)
        {
            Logger.logBegin("GetObjectTypeList");
            if (typeDicList == null)
            {
                LoadTypeDicFromDB(strDBIdx);
            }
            return typeDicList;
        }

        private static void LoadTypeDicFromDB(string strDBIdx)
        {
            typeDicList = new ObservableCollection<B_GUI_COMPONENT_TYPE_DIC>();
            Logger.logBegin("LoadTypeDicFromDB");
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var typeNames = (from c in marsEntities.T_GUI_COMPONENT_TYPE_DIC
                                 select c);
                List<B_GUI_COMPONENT_TYPE_DIC> lstTmp = new List<B_GUI_COMPONENT_TYPE_DIC>();
                foreach (T_GUI_COMPONENT_TYPE_DIC typeName in typeNames)
                {
                    B_GUI_COMPONENT_TYPE_DIC guiType = B_GUI_COMPONENT_TYPE_DIC.CreateFromDTO(T_GUI_COMPONENT_TYPE_DICAssembler.ToDTO(typeName));

                    lstTmp.Add(guiType);
                }

                typeDicList = new ObservableCollection<B_GUI_COMPONENT_TYPE_DIC>(lstTmp.OrderBy(p => p.TYPE_NAME));
            }
            catch (Exception e)
            {
                Logger.Error("LoadTypeDicFromDB", string.Format("Exception:[{0}]", e.Message), e);
                typeDicList.Clear();
            }
        }

        private static B_GUI_COMPONENT_TYPE_DIC CreateFromDTO(T_GUI_COMPONENT_TYPE_DICDTO objDto)
        {
            Logger.Info("CreateFromDTO", string.Format("objDTO.type:[{0}]", objDto == null ? "" : objDto.TYPE_NAME));
            B_GUI_COMPONENT_TYPE_DIC objResult = new B_GUI_COMPONENT_TYPE_DIC();
            objResult.TYPE_ID = objDto.TYPE_ID;
            objResult.TYPE_NAME = objDto.TYPE_NAME;
            objResult.T_DIC_RELATION_KEYWORD_RELATION_ID = objDto.T_DIC_RELATION_KEYWORD_RELATION_ID;
            objResult.T_REGISTED_OBJECT_OBJECT_ID = objDto.T_REGISTED_OBJECT_OBJECT_ID;
            return objResult;
        }

        public static string GetObjectTypeById(string strDBIdx,long? lTypId, ref bool isOk, ref string strError)
        {
            ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> lstDic = GetObjectTypeListEx(strDBIdx);
            if (lstDic == null)
            {
                isOk = false;
                Logger.Error("GetObjectTypeById", strError = "Can't get Type list from DB and cache");
                return "";
            }

            B_GUI_COMPONENT_TYPE_DIC objDic = lstDic.Where(p => p.TYPE_ID == lTypId).FirstOrDefault();
            if (objDic == null)
            {
                isOk = false;
                Logger.Error("GetObjectTypeById", strError = string.Format("No such type id exists--[{0}]", lTypId ?? -1));
                return "";
            }
            isOk = true;
            return objDic.TYPE_NAME;
        }

        public static int GetObjectTypeIDByName(string strDBIdx,string objectTestType, ref bool isOk, ref string strError)
        {
            B_GUI_COMPONENT_TYPE_DIC objTmp = new B_GUI_COMPONENT_TYPE_DIC();
            var typ = objTmp.GetObjectTypeList(strDBIdx).Where(p => string.Compare(p.TYPE_NAME, objectTestType, true) == 0).FirstOrDefault();
            if (typ != null)
            {
                isOk = true;
                return (int)typ.TYPE_ID;
            }
            else
            {
                isOk = false;
                strError = string.Format("No such [{0}] type exists", objectTestType);
                return -1;
            }
        }
    }
}
