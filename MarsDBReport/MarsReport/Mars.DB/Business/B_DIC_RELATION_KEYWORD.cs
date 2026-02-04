using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using System;
using System.Data.Objects;

namespace Mars.Business
{
    public class B_DIC_RELATION_KEYWORD : T_DIC_RELATION_KEYWORDDTO
    {
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
    }
}
