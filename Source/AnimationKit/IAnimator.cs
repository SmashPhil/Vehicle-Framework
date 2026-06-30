using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace AnimationKit;

[PublicAPI]
public interface IAnimator
{
  int EntityId { get; }
}
