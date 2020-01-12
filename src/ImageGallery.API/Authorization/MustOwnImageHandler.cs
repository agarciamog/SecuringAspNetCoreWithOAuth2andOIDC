using ImageGallery.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGallery.API.Authorization
{
    public class MustOwnImageHandler : AuthorizationHandler<MustOwnImageRequirement>
    {
        private readonly IGalleryRepository galleryRepository;

        public MustOwnImageHandler(IGalleryRepository galleryRepository)
        {
            this.galleryRepository = galleryRepository;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MustOwnImageRequirement requirement)
        {
            // are they authorized, if not then null so fail
            var filterContext = context.Resource as AuthorizationFilterContext;
            if (filterContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // what are they trying to access, and if no id
            // is found then fail
            var imageId = filterContext.RouteData.Values["id"].ToString();

            if(!Guid.TryParse(imageId, out Guid imageIdAsGuid))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // who is trying to access the resource
            var ownerId = context.User.Claims.FirstOrDefault(c => c.Type == "sub").Value;
            
            // does the the ownerId own the image?
            if(!galleryRepository.IsImageOwner(ownerId, imageIdAsGuid))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
