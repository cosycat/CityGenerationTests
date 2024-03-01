using System;

namespace Simulation {
    
    public abstract class RoadDeveloperAgent : Agent {
        
        public RoadDeveloperAgent(LandUsage usageType, ISite currSite) : base(usageType, currSite) { }
        
        protected internal override void UpdateTick() {
            MoveToNewLocation();
            if (LocationMeetsConstraint() && LocationNeedsRoad()) {
                var newRoadSegment = BuildRoad();
                if (IsValidRoad(newRoadSegment)) {
                    Commit(newRoadSegment);
                }
            }
        }

        protected virtual void MoveToNewLocation() {
            throw new NotImplementedException();
        }
        
        protected virtual bool LocationMeetsConstraint() {
            throw new NotImplementedException();
        }
        
        protected virtual bool LocationNeedsRoad() {
            throw new NotImplementedException();
        }

        protected abstract RoadSegment BuildRoad();
        
        protected virtual bool IsValidRoad(RoadSegment newRoadSegment) {
            throw new NotImplementedException();
        }
        
        protected virtual void Commit(RoadSegment newRoadSegment) {
            throw new NotImplementedException();
        }
        
    }

    public class TertiaryRoadExtender : RoadDeveloperAgent {
        
        public TertiaryRoadExtender(LandUsage usageType, ISite currSite) : base(usageType, currSite) {
            
        }


        protected override RoadSegment BuildRoad() {
            throw new NotImplementedException();
        }
    }
}