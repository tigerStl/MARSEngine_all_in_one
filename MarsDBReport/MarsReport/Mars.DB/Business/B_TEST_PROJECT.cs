using Mars.DataLayer;
using Mars.DataLayer.Generic;
using Mars.Dto;
using Mars.Model;

using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Objects;
using System.Linq;

namespace Mars.Business
{
    public class B_TEST_PROJECT : T_TEST_PROJECTDTO, IMarsTigerTranscation, INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_PROJECT));

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }

        }

        public override Int64 PROJECT_ID
        {
            get
            {
                return base.PROJECT_ID;
            }
            set
            {
                base.PROJECT_ID = value;
                RaisePropertyChanged("PROJECT_ID");
            }
        }

        public override String PROJECT_NAME
        {
            get
            {
                return base.PROJECT_NAME;
            }
            set
            {
                base.PROJECT_NAME = value;
                RaisePropertyChanged("PROJECT_NAME");
            }
        }
        public override String PROJECT_DESCRIPTION
        {
            get
            {
                return base.PROJECT_DESCRIPTION;
            }
            set
            {
                base.PROJECT_DESCRIPTION = value;
                RaisePropertyChanged("PROJECT_DESCRIPTION");
            }
        }
        public override String CREATOR
        {
            get
            {
                return base.CREATOR;
            }
            set
            {
                base.CREATOR = value;
                RaisePropertyChanged("CREATOR");
            }
        }

        public override Nullable<DateTime> CREATE_DATE
        {
            get
            {
                return base.CREATE_DATE;
            }
            set
            {
                base.CREATE_DATE = value;
                RaisePropertyChanged("CREATE_DATE");
            }
        }
        public override Nullable<Int16> STATUS
        {
            get
            {
                return base.STATUS;
            }
            set
            {
                base.STATUS = value;
                RaisePropertyChanged("STATUS");
            }
        }


#if v_16AndUp
        public List<long?> AssignedAppIds;
#endif 
        public long getProjectId(string strDBIdx, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("T_TEST_PROJECT_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public long getProjectId(string strDBIdx, string projectName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            long projectId = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_NAME == projectName).PROJECT_ID;
            return projectId;

        }

        public T_TEST_PROJECTDTO GetProject(string strDBIdx,long lProjId, bool isIncludeApp = false)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (!isIncludeApp)
            {
                var q = (from p in marsEntities.T_TEST_PROJECT
                         where p.PROJECT_ID == lProjId
                         select p).FirstOrDefault();
                return q.ToDTO();
            }
            else
            {
                var q = (from p in marsEntities.T_TEST_PROJECT.Include("REL_APP_PROJ")
                         where p.PROJECT_ID == lProjId
                         select p).FirstOrDefault();
                return q.ToDTO();
            }
        }

        public B_TEST_PROJECT GetProjectBOById(string strDBIdx, long lProjId)
        {
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var q = (from p in marsEntities.T_TEST_PROJECT.Include("REL_APP_PROJ")
                         where p.PROJECT_ID == lProjId
                         select p).FirstOrDefault();
                if (q == null) return null;
                B_TEST_PROJECT newProject = B_TEST_PROJECT.CreateObjectFromDto(T_TEST_PROJECTAssembler.ToDTO(q));
                newProject.AssignedAppIds = q.REL_APP_PROJ == null ? null :
                        (from r in q.REL_APP_PROJ
                         select r.APPLICATION_ID).Distinct().ToList();
                return newProject;
            }
            catch (Exception e)
            {
                Logger.Error("GetProjectById", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        public List<B_TEST_PROJECT> GetProject(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<B_TEST_PROJECT> Project = new List<B_TEST_PROJECT>();
            var project = (from c in marsEntities.T_TEST_PROJECT.Include("REL_APP_PROJ")
                           orderby c.PROJECT_NAME
                           select c);

            foreach (T_TEST_PROJECT regProject in project)
            {
                B_TEST_PROJECT newProject = B_TEST_PROJECT.CreateObjectFromDto(T_TEST_PROJECTAssembler.ToDTO(regProject));
#if v_16AndUp
                newProject.AssignedAppIds = regProject.REL_APP_PROJ == null ? null :
                    (from r in regProject.REL_APP_PROJ
                     select r.APPLICATION_ID).Distinct().ToList();
#endif
                //newProject.PROJECT_ID = regProject.PROJECT_ID;
                //newProject.PROJECT_NAME = regProject.PROJECT_NAME;
                //newProject.PROJECT_DESCRIPTION = regProject.PROJECT_DESCRIPTION;
                Project.Add(newProject);
            }
            return Project;
        }

        public bool AddNewObject2Database(string strDBIdx, MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("AddNewObject2Database");
            try
            {
                PROJECT_ID = getProjectId(strDBIdx,dbCntx);
                this.CREATE_DATE = DateTime.Now;
                dbCntx.T_TEST_PROJECT.Add(this.ToEntity());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AddNewObject2Database", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("AddNewObject2Database");
            }
        }

        public bool ProjectExists(string strDBIdx, string projectName, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var project = (from c in marsEntities.T_TEST_PROJECT
                           where c.PROJECT_NAME.ToUpper() == projectName.ToUpper()
                           select c);
            if (project != null && project.Count() > 0)
            {
                return true;
            }
            return false;
        }

        private static B_TEST_PROJECT CreateObjectFromDto(T_TEST_PROJECTDTO objDto)
        {
            B_TEST_PROJECT objResult = new B_TEST_PROJECT();
            if (objDto == null) return null;
            objResult.CREATE_DATE = objDto.CREATE_DATE;
            objResult.CREATOR = objDto.CREATOR;
            objResult.PROJECT_DESCRIPTION = objDto.PROJECT_DESCRIPTION;
            objResult.PROJECT_ID = objDto.PROJECT_ID;
            objResult.PROJECT_NAME = objDto.PROJECT_NAME;
            objResult.REL_APP_PROJ_RELATIONSHIP_ID = objDto.REL_APP_PROJ_RELATIONSHIP_ID;
            objResult.REL_TEST_SUIT_PROJECT_RELATIONSHIP_ID = objDto.REL_TEST_SUIT_PROJECT_RELATIONSHIP_ID;
#if db4Oracle
            objResult.T_PROJECT_DATA_SOURCE_PRO_DBS_ID = objDto.T_PROJECT_DATA_SOURCE_PRO_DBS_ID;
#endif
            objResult.T_PROJ_TC_MGR_STORYBOARD_DETAIL_ID = objDto.T_PROJ_TC_MGR_STORYBOARD_DETAIL_ID;
            return objResult;
        }

        public static B_TEST_PROJECT GetProjectById(string strDBIdx, long projectId)
        {
            B_TEST_PROJECT objResultDTO = null;
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var objRslt = marsEntities.T_TEST_PROJECT.FirstOrDefault(p => p.PROJECT_ID == projectId);

                if (objRslt == null) return null;

                return objResultDTO = CreateObjectFromDto(T_TEST_PROJECTAssembler.ToDTO(objRslt));
            }
            catch (Exception e)
            {
                Logger.Error("GetProjectById", string.Format("Try get project by Id:[{0}], Exception:[{1}]", projectId, e.Message), e);
                return null;
            }
            finally
            {
                Logger.Info("GetProjectById", string.Format("Try get project by Id:[{0}],returns:[{1}]", projectId, objResultDTO == null ? "null" : objResultDTO.ToString()));
            }

        }

        public Type GetBOEntityType()
        {
            return typeof(T_TEST_PROJECT);
        }

        public bool ModifyObject(MarsEntities objEntityMgr, ref string strError)
        {
            //Logger.Error("ModifyObject",);
            if (objEntityMgr == null)
            {
                Logger.Error("ModifyObject", strError = "EntityFrame work is null");
                return false;
            }
            var q = objEntityMgr.Set<T_TEST_PROJECT>();
            T_TEST_PROJECT objPrj = q.FirstOrDefault(p => p.PROJECT_ID == this.PROJECT_ID);
            if (objPrj == null)
            {
                Logger.Error("ModifyObject", strError = string.Format("No such record for project id:[{0}]", this.PROJECT_ID));
                return false;
            }

            q.Attach(objPrj);
            copy2ExistEntityExceptId(objPrj);
            return true;

        }

        internal static B_TEST_PROJECT ConverFromDTO(T_TEST_PROJECTDTO srcDto)
        {
            if (srcDto == null) return null;
            B_TEST_PROJECT testProj = new B_TEST_PROJECT();
            testProj.CREATE_DATE = srcDto.CREATE_DATE;
            testProj.CREATOR = srcDto.CREATOR;
            testProj.PROJECT_DESCRIPTION = srcDto.PROJECT_DESCRIPTION;
            testProj.PROJECT_ID = srcDto.PROJECT_ID;
            testProj.PROJECT_NAME = srcDto.PROJECT_NAME;
            testProj.STATUS = srcDto.STATUS;
            return testProj;
        }

        private void copy2ExistEntityExceptId(T_TEST_PROJECT objDes)
        {
            objDes.CREATE_DATE = this.CREATE_DATE;
            objDes.CREATOR = this.CREATOR;
            objDes.PROJECT_DESCRIPTION = this.PROJECT_DESCRIPTION;

            objDes.PROJECT_NAME = this.PROJECT_NAME;
            objDes.STATUS = this.STATUS;

        }

        public EN_TRANSCATION_DEALSTAUTS RemoveByTranscation(MarsEntities transcationEntityMgr, ref string strError)
        {
            strError = "Not implement!!!!!";
            return EN_TRANSCATION_DEALSTAUTS.EN_IGNORE;
        }
        /// <summary>
        /// Create a new entity and save to Database
        /// </summary>
        /// <param name="transcationEntityMgr"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public EN_TRANSCATION_DEALSTAUTS AddByTranscation(string strDBIdx, MarsEntities transcationEntityMgr, ref string strError)
        {
            if (transcationEntityMgr == null)
            {
                strError = "Transcation Mgr object is null.";
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }
            try
            {

                bool isOk = transcationEntityMgr.REL_APP_PROJ.Any(p => p.PROJECT_ID == this.PROJECT_ID);
                if (isOk)
                {
                    strError = string.Format("A recorder is Exists, PROJECT_ID id:[{0}]", this.PROJECT_ID);
                    return EN_TRANSCATION_DEALSTAUTS.EN_IGNORE;
                }
                if (this.PROJECT_ID < 0)
                {
                    this.PROJECT_ID = getProjectId(strDBIdx);
                }

                transcationEntityMgr.T_TEST_PROJECT.Add(this.ToEntity());
                return EN_TRANSCATION_DEALSTAUTS.EN_OK;
            }
            catch (Exception e)
            {
                Logger.Error("AddByTranscation", strError = string.Format("Exception:[{0}]", e.Message), e);
                return EN_TRANSCATION_DEALSTAUTS.EN_ERROR;
            }

        }

        private static System.Collections.ObjectModel.ObservableCollection<B_TEST_PROJECT> _CachedProjects;
        public static System.Collections.ObjectModel.ObservableCollection<B_TEST_PROJECT> getCachedProjects(string strDBIdx)
        {
        //    get
            {
                if (_CachedProjects == null)
                {
                    try
                    {
                        MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                        var p = from q in dbCntx.T_TEST_PROJECT
                                orderby q.PROJECT_NAME
                                select q;
                        List<B_TEST_PROJECT> lstRslt = new List<B_TEST_PROJECT>();
                        foreach (var itm in p)
                        {
                            if (itm == null) continue;
                            lstRslt.Add(ConvertFromDTO(itm.ToDTO()));
                        }
                        return _CachedProjects = new System.Collections.ObjectModel.ObservableCollection<B_TEST_PROJECT>(lstRslt);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("CachedProjects_get", string.Format("Exception:[{0}]", e.Message), e);
                        return _CachedProjects = null;
                    }
                }
                return _CachedProjects;
            }
        }

        private static B_TEST_PROJECT ConvertFromDTO(T_TEST_PROJECTDTO obj)
        {
            if (obj == null) return null;
            B_TEST_PROJECT rslt = new B_TEST_PROJECT();
            rslt.CREATE_DATE = obj.CREATE_DATE;
            rslt.CREATOR = obj.CREATOR;
            rslt.PROJECT_DESCRIPTION = obj.PROJECT_DESCRIPTION;
            rslt.PROJECT_ID = obj.PROJECT_ID;
            rslt.PROJECT_NAME = obj.PROJECT_NAME;
            rslt.REL_APP_PROJ_RELATIONSHIP_ID = obj.REL_APP_PROJ_RELATIONSHIP_ID;
            rslt.STATUS = obj.STATUS;
            return rslt;
        }
    }
}
