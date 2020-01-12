﻿using ImageGallery.API.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageGallery.API.Services
{
    public class GalleryRepository : IGalleryRepository, IDisposable
    {
        GalleryContext _context;

        public GalleryRepository(GalleryContext galleryContext)
        {
            _context = galleryContext;
        }
        public bool ImageExists(Guid id)
        {
            return _context.Images.Any(i => i.Id == id);
        }
        
        public Image GetImage(Guid id)
        {
            return _context.Images.FirstOrDefault(i => i.Id == id);
        }
  
        public IEnumerable<Image> GetImages(string ownerId)
        {
            return _context.Images
                .Where(i => i.OwnerId == ownerId)
                .OrderBy(i => i.Title).ToList();
        }

        public void AddImage(Image image)
        {
            _context.Images.Add(image);
        }

        public void UpdateImage(Image image)
        {
            // no code in this implementation
        }

        public void DeleteImage(Image image)
        {
            _context.Images.Remove(image);

            // Note: in a real-life scenario, the image itself should also 
            // be removed from disk.  We don't do this in this demo
            // scenario, as we refill the DB with image URIs (that require
            // the actual files as well) for demo purposes.
        }

        public bool Save()
        {
            return (_context.SaveChanges() >= 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }

            }
        }

        public bool IsImageOwner(string ownerId, Guid imageId)
        {
            return _context.Images.Any(i => i.OwnerId == ownerId && i.Id == imageId);
        }
    }
}
