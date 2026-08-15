using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public string UserName { get; set; }
        public Guid PostId { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsAuthor { get; set; }
        public Guid UserId { get; set; }

        // Reply 
        public Guid? ParentCommentId { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = new List<CommentResponseDto>();
        public int ReplyCount { get; set; }
        
        
        // Nesting depth: 1 = top-level comment, 2 = reply, 3 = reply-on-reply, max 4.
        public int Depth { get; set; } = 1;
    }
}
