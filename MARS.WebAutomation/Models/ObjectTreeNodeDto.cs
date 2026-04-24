using System.Collections.Generic;

namespace MARS.WebAutomation.Models
{
    public sealed class ObjectTreeNodeDto
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string DisplayName { get; set; }
        public string Tag { get; set; }
        public string Role { get; set; }
        public string LocatorHint { get; set; }
        public BoundingRectDto Bounds { get; set; }
        public List<ObjectTreeNodeDto> Children { get; set; } = new List<ObjectTreeNodeDto>();
    }
}
